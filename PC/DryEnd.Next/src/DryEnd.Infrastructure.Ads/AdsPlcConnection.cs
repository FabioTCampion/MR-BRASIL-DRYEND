using DryEnd.Application;
using DryEnd.Domain;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.TypeSystem;

namespace DryEnd.Infrastructure.Ads;

public sealed class AdsPlcConnection : IPlcConnection, IPlcOrderEditor, IPlcNextOrderWriter, IPlcChangeOrderAcknowledger, IPlcOrderCommandWriter
{
    private readonly AdsClient _client = new();
    private readonly AdsClient _commandClient = new();
    private readonly AdsOptions _options;
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private ISymbol? _currentOrderSymbol;
    private ISymbol? _nextOrderSymbol;

    public AdsPlcConnection(AdsOptions options)
    {
        options.Validate();
        _options = options;
    }

    public bool IsConnected => _client.IsConnected;

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        var address = new AmsNetId(_options.AmsNetId);
        if (!IsConnected)
            await _client.ConnectAsync(address, _options.Port, cancellationToken);
        if (!_commandClient.IsConnected)
            await _commandClient.ConnectAsync(address, _options.Port, cancellationToken);

        EnsureOrderSymbolsLoaded();
    }

    public async Task<PlcDataSnapshot> ReadSnapshotAsync(CancellationToken cancellationToken)
    {
        if (!IsConnected)
            throw new InvalidOperationException("ADS client is not connected.");

        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            var current = await ReadOrderAsync(_options.CurrentOrderRoot, cancellationToken);
            var next = await ReadOrderAsync(_options.NextOrderRoot, cancellationToken);
            return new PlcDataSnapshot(current, next, DateTimeOffset.UtcNow);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task<OrderSnapshot> UpdateCurrentOrderAsync(
        CurrentOrderUpdate update,
        CancellationToken cancellationToken)
    {
        if (!IsConnected)
            throw new InvalidOperationException("ADS client is not connected.");

        update.ValidateStructure();
        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            await WriteOrderUpdateAsync(_options.CurrentOrderRoot, update, cancellationToken);
            return await ReadOrderAsync(_options.CurrentOrderRoot, cancellationToken);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task<OrderSnapshot> PatchCurrentOrderAsync(
        CurrentOrderPatch patch,
        CancellationToken cancellationToken)
    {
        if (!IsConnected)
            throw new InvalidOperationException("ADS client is not connected.");

        patch.ValidateStructure();
        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            await WriteOrderPatchAsync(
                _options.CurrentOrderRoot,
                patch.BaseSnapshot,
                patch.UpdatedOrder,
                cancellationToken);
            return await ReadOrderAsync(_options.CurrentOrderRoot, cancellationToken);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task<OrderSnapshot> WriteNextOrderAsync(
        NextOrderUpdate update,
        CancellationToken cancellationToken)
    {
        if (!IsConnected)
            throw new InvalidOperationException("ADS client is not connected.");

        update.ValidateStructure();
        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            await WriteNextOrderUpdateAsync(_options.NextOrderRoot, update, cancellationToken);
            return await ReadOrderAsync(_options.NextOrderRoot, cancellationToken);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task AcknowledgeChangeOrderAsync(
        DateTime nextOrderStartedAt,
        CancellationToken cancellationToken)
    {
        if (!IsConnected)
            throw new InvalidOperationException("ADS client is not connected.");

        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            var startedAtText = nextOrderStartedAt.ToString("yyyy-MM-dd HH:mm:ss");
            await WriteValueAsync($"{_options.NextOrderRoot}.startedAt", startedAtText, cancellationToken);
            await WriteValueAsync($"{_options.CurrentOrderRoot}.saveSQLFinished", true, cancellationToken);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public Task<bool> RequestChangeOrderAsync(CancellationToken cancellationToken) =>
        RequestOrderCommandAsync("changeOrderRequest", cancellationToken);

    public Task<bool> RequestAutomaticOrderChangeAsync(CancellationToken cancellationToken) =>
        RequestOrderCommandAsync("aocRequest", cancellationToken);

    private async Task<bool> RequestOrderCommandAsync(
        string commandName,
        CancellationToken cancellationToken)
    {
        if (!_commandClient.IsConnected)
            await _commandClient.ConnectAsync(
                new AmsNetId(_options.AmsNetId),
                _options.Port,
                cancellationToken);

        var symbol = $"{_options.CurrentOrderRoot}.{commandName}";
        var writeResult = await _commandClient.WriteValueAsync(symbol, true, cancellationToken);
        if (writeResult.Failed)
            throw new AdsErrorException($"Could not write ADS symbol '{symbol}'.", writeResult.ErrorCode);

        var readResult = await _commandClient.ReadValueAsync<bool>(symbol, cancellationToken);
        if (readResult.Failed)
            throw new AdsErrorException($"Could not read ADS symbol '{symbol}'.", readResult.ErrorCode);
        if (!readResult.Value)
            throw new InvalidOperationException($"ADS readback did not confirm '{commandName}'.");
        return true;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        _currentOrderSymbol = null;
        _nextOrderSymbol = null;
        _client.Disconnect();
        _commandClient.Disconnect();
        return Task.CompletedTask;
    }

    public IReadOnlyList<string> FindSymbolPaths(string searchText)
    {
        if (!IsConnected)
            return [];

        var loader = SymbolLoaderFactory.Create(_client, SymbolLoaderSettings.Default);
        return loader.Symbols
            .SelectMany(Flatten)
            .Select(symbol => symbol.InstancePath)
            .Where(path => path.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(200)
            .ToArray();
    }

    private static IEnumerable<TwinCAT.TypeSystem.ISymbol> Flatten(TwinCAT.TypeSystem.ISymbol symbol)
    {
        yield return symbol;
        foreach (var child in symbol.SubSymbols)
            foreach (var descendant in Flatten(child))
                yield return descendant;
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task WriteOrderUpdateAsync(
        string root,
        CurrentOrderUpdate update,
        CancellationToken cancellationToken)
    {
        await WriteValueAsync($"{root}.startedAt", update.StartedAt ?? string.Empty, cancellationToken);
        await WriteValueAsync($"{root}.productionListNumber", update.ProductionListNumber, cancellationToken);
        await WriteValueAsync($"{root}.levelSelector", update.LevelSelector, cancellationToken);
        await WriteValueAsync($"{root}.paperComposition", update.PaperComposition ?? string.Empty, cancellationToken);
        await WriteValueAsync($"{root}.fluteType", update.FluteType ?? string.Empty, cancellationToken);
        await WriteValueAsync($"{root}.paperWidth", update.PaperWidth, cancellationToken);

        for (var index = 1; index <= update.PaperLayers.Count; index++)
            await WriteValueAsync(
                $"{root}.paper{index}",
                update.PaperLayers[index - 1] ?? string.Empty,
                cancellationToken);

        await WriteValueAsync($"{root}.linearMeters", update.LinearMeters, cancellationToken);
        await WriteValueAsync($"{root}.linearMetersProduced", update.LinearMetersProduced, cancellationToken);
        await WriteValueAsync($"{root}.linearMetersRemaining", update.LinearMetersRemaining, cancellationToken);
        await WriteValueAsync($"{root}.scorerHeightMM", update.ScorerHeightMm, cancellationToken);
        await WriteValueAsync($"{root}.invertOrderLevel", update.InvertOrderLevel, cancellationToken);
        await WriteValueAsync($"{root}.invertOrderSide", update.InvertOrderSide, cancellationToken);
        await WriteOrderChannelUpdateAsync($"{root}.order1", update.Order1, cancellationToken);
        await WriteOrderChannelUpdateAsync($"{root}.order2", update.Order2, cancellationToken);

        // EN: Write the database identity last so it acts as the final field of the update.
        // PT: Grava a identidade do banco por último para que ela seja o campo final da atualização.
        await WriteValueAsync($"{root}.tableID", update.TableId, cancellationToken);
    }

    private async Task WriteOrderPatchAsync(
        string root,
        CurrentOrderUpdate original,
        CurrentOrderUpdate updated,
        CancellationToken cancellationToken)
    {
        await WriteIfChangedAsync($"{root}.startedAt", original.StartedAt, updated.StartedAt, cancellationToken);
        await WriteIfChangedAsync($"{root}.productionListNumber", original.ProductionListNumber, updated.ProductionListNumber, cancellationToken);
        await WriteIfChangedAsync($"{root}.levelSelector", original.LevelSelector, updated.LevelSelector, cancellationToken);
        await WriteIfChangedAsync($"{root}.paperComposition", original.PaperComposition, updated.PaperComposition, cancellationToken);
        await WriteIfChangedAsync($"{root}.fluteType", original.FluteType, updated.FluteType, cancellationToken);
        await WriteIfChangedAsync($"{root}.paperWidth", original.PaperWidth, updated.PaperWidth, cancellationToken);

        for (var index = 1; index <= updated.PaperLayers.Count; index++)
            await WriteIfChangedAsync(
                $"{root}.paper{index}",
                original.PaperLayers[index - 1],
                updated.PaperLayers[index - 1],
                cancellationToken);

        await WriteIfChangedAsync($"{root}.linearMeters", original.LinearMeters, updated.LinearMeters, cancellationToken);
        await WriteIfChangedAsync($"{root}.linearMetersProduced", original.LinearMetersProduced, updated.LinearMetersProduced, cancellationToken);
        await WriteIfChangedAsync($"{root}.linearMetersRemaining", original.LinearMetersRemaining, updated.LinearMetersRemaining, cancellationToken);
        await WriteIfChangedAsync($"{root}.scorerHeightMM", original.ScorerHeightMm, updated.ScorerHeightMm, cancellationToken);
        await WriteIfChangedAsync($"{root}.invertOrderLevel", original.InvertOrderLevel, updated.InvertOrderLevel, cancellationToken);
        await WriteIfChangedAsync($"{root}.invertOrderSide", original.InvertOrderSide, updated.InvertOrderSide, cancellationToken);
        await WriteOrderChannelPatchAsync($"{root}.order1", original.Order1, updated.Order1, cancellationToken);
        await WriteOrderChannelPatchAsync($"{root}.order2", original.Order2, updated.Order2, cancellationToken);

        // EN: If changed, the database identity remains the commit marker and is written last.
        // PT: Se alterada, a identidade do banco continua sendo o marcador final e e gravada por ultimo.
        await WriteIfChangedAsync($"{root}.tableID", original.TableId, updated.TableId, cancellationToken);
    }

    private async Task WriteOrderChannelPatchAsync(
        string root,
        OrderChannelUpdate original,
        OrderChannelUpdate updated,
        CancellationToken cancellationToken)
    {
        await WriteIfChangedAsync($"{root}.id", original.Id, updated.Id, cancellationToken);
        await WriteIfChangedAsync($"{root}.product", original.Product, updated.Product, cancellationToken);
        await WriteIfChangedAsync($"{root}.client", original.Client, updated.Client, cancellationToken);
        await WriteIfChangedAsync($"{root}.sheetType", original.SheetType, updated.SheetType, cancellationToken);
        await WriteIfChangedAsync($"{root}.sheetQuantity", original.SheetQuantity, updated.SheetQuantity, cancellationToken);
        await WriteIfChangedAsync($"{root}.sheetLength", original.SheetLength, updated.SheetLength, cancellationToken);

        for (var index = 1; index <= updated.SheetMeasures.Count; index++)
            await WriteIfChangedAsync(
                $"{root}.sheetM{index}",
                original.SheetMeasures[index - 1],
                updated.SheetMeasures[index - 1],
                cancellationToken);

        await WriteIfChangedAsync($"{root}.numberOfCuts", original.NumberOfCuts, updated.NumberOfCuts, cancellationToken);
        await WriteIfChangedAsync($"{root}.numberOfCutsProduced", original.NumberOfCutsProduced, updated.NumberOfCutsProduced, cancellationToken);
        await WriteIfChangedAsync($"{root}.numberOfCutsRemaining", original.NumberOfCutsRemaining, updated.NumberOfCutsRemaining, cancellationToken);
        await WriteIfChangedAsync($"{root}.pileQuantity", original.PileQuantity, updated.PileQuantity, cancellationToken);
        await WriteIfChangedAsync($"{root}.pileQuantityProduced", original.PileQuantityProduced, updated.PileQuantityProduced, cancellationToken);
        await WriteIfChangedAsync($"{root}.pileQuantityRemaining", original.PileQuantityRemaining, updated.PileQuantityRemaining, cancellationToken);
        await WriteIfChangedAsync($"{root}.pileCounter", original.PileCounter, updated.PileCounter, cancellationToken);
        await WriteIfChangedAsync($"{root}.scrapCounter", original.ScrapCounter, updated.ScrapCounter, cancellationToken);
    }

    private async Task WriteOrderChannelUpdateAsync(
        string root,
        OrderChannelUpdate update,
        CancellationToken cancellationToken)
    {
        await WriteValueAsync($"{root}.id", update.Id, cancellationToken);
        await WriteValueAsync($"{root}.product", update.Product ?? string.Empty, cancellationToken);
        await WriteValueAsync($"{root}.client", update.Client ?? string.Empty, cancellationToken);
        await WriteValueAsync($"{root}.sheetType", update.SheetType, cancellationToken);
        await WriteValueAsync($"{root}.sheetQuantity", update.SheetQuantity, cancellationToken);
        await WriteValueAsync($"{root}.sheetLength", update.SheetLength, cancellationToken);

        for (var index = 1; index <= update.SheetMeasures.Count; index++)
            await WriteValueAsync(
                $"{root}.sheetM{index}",
                update.SheetMeasures[index - 1],
                cancellationToken);

        await WriteValueAsync($"{root}.numberOfCuts", update.NumberOfCuts, cancellationToken);
        await WriteValueAsync($"{root}.numberOfCutsProduced", update.NumberOfCutsProduced, cancellationToken);
        await WriteValueAsync($"{root}.numberOfCutsRemaining", update.NumberOfCutsRemaining, cancellationToken);
        await WriteValueAsync($"{root}.pileQuantity", update.PileQuantity, cancellationToken);
        await WriteValueAsync($"{root}.pileQuantityProduced", update.PileQuantityProduced, cancellationToken);
        await WriteValueAsync($"{root}.pileQuantityRemaining", update.PileQuantityRemaining, cancellationToken);
        await WriteValueAsync($"{root}.pileCounter", update.PileCounter, cancellationToken);
        await WriteValueAsync($"{root}.scrapCounter", update.ScrapCounter, cancellationToken);
    }

    private async Task WriteNextOrderUpdateAsync(
        string root,
        NextOrderUpdate update,
        CancellationToken cancellationToken)
    {
        await WriteValueAsync($"{root}.startedAt", string.Empty, cancellationToken);
        await WriteValueAsync($"{root}.productionListNumber", update.ProductionListNumber, cancellationToken);
        await WriteValueAsync($"{root}.levelSelector", update.LevelSelector, cancellationToken);
        await WriteValueAsync($"{root}.paperComposition", update.PaperComposition, cancellationToken);
        await WriteValueAsync($"{root}.fluteType", update.FluteType, cancellationToken);
        await WriteValueAsync($"{root}.paperWidth", update.PaperWidth, cancellationToken);

        for (var index = 1; index <= update.PaperLayers.Count; index++)
            await WriteValueAsync($"{root}.paper{index}", update.PaperLayers[index - 1], cancellationToken);

        await WriteNextOrderChannelAsync($"{root}.order1", update.Order1, cancellationToken);
        await WriteNextOrderChannelAsync($"{root}.order2", update.Order2, cancellationToken);

        // EN: The database ID is the commit marker and must always be written last.
        // PT: O ID do banco funciona como marcador de conclusao e deve ser gravado por ultimo.
        await WriteValueAsync($"{root}.tableID", update.TableId, cancellationToken);
    }

    private async Task WriteNextOrderChannelAsync(
        string root,
        NextOrderChannelUpdate update,
        CancellationToken cancellationToken)
    {
        await WriteValueAsync($"{root}.id", update.Id, cancellationToken);
        await WriteValueAsync($"{root}.product", update.Product, cancellationToken);
        await WriteValueAsync($"{root}.client", update.Client, cancellationToken);
        await WriteValueAsync($"{root}.sheetType", update.SheetType, cancellationToken);
        await WriteValueAsync($"{root}.sheetQuantity", update.SheetQuantity, cancellationToken);
        await WriteValueAsync($"{root}.sheetLength", update.SheetLength, cancellationToken);

        for (var index = 1; index <= update.SheetMeasures.Count; index++)
            await WriteValueAsync($"{root}.sheetM{index}", update.SheetMeasures[index - 1], cancellationToken);

        await WriteValueAsync($"{root}.numberOfCuts", update.NumberOfCuts, cancellationToken);
        await WriteValueAsync($"{root}.numberOfCutsProduced", 0, cancellationToken);
        await WriteValueAsync($"{root}.numberOfCutsRemaining", 0, cancellationToken);
        await WriteValueAsync($"{root}.pileQuantity", update.PileQuantity, cancellationToken);
        await WriteValueAsync($"{root}.pileQuantityProduced", (short)0, cancellationToken);
        await WriteValueAsync($"{root}.pileQuantityRemaining", (short)0, cancellationToken);
        await WriteValueAsync($"{root}.pileCounter", (short)0, cancellationToken);
        await WriteValueAsync($"{root}.scrapCounter", (short)0, cancellationToken);
        await WriteValueAsync($"{root}.counterReset", false, cancellationToken);
    }

    private async Task<OrderSnapshot> ReadOrderAsync(string root, CancellationToken cancellationToken)
    {
        EnsureOrderSymbolsLoaded();
        var symbol = string.Equals(root, _options.CurrentOrderRoot, StringComparison.OrdinalIgnoreCase)
            ? _currentOrderSymbol!
            : _nextOrderSymbol!;
        if (symbol is not IValueSymbol valueSymbol)
            throw new InvalidOperationException($"ADS structure symbol '{root}' is not readable.");
        var result = await valueSymbol.ReadValueAsync(cancellationToken);
        if (result.Failed)
            throw new AdsErrorException($"Could not read ADS structure '{root}'.", (AdsErrorCode)result.ErrorCode);

        return BulkOrderSnapshotMapper.Map(result.Value, root);
    }

    private void EnsureOrderSymbolsLoaded()
    {
        if (_currentOrderSymbol is not null && _nextOrderSymbol is not null)
            return;

        var loader = SymbolLoaderFactory.Create(_client, SymbolLoaderSettings.DefaultDynamic);
        var symbols = loader.Symbols.SelectMany(Flatten).ToArray();
        _currentOrderSymbol = FindRootSymbol(symbols, _options.CurrentOrderRoot);
        _nextOrderSymbol = FindRootSymbol(symbols, _options.NextOrderRoot);
    }

    private static ISymbol FindRootSymbol(IEnumerable<ISymbol> symbols, string instancePath) =>
        symbols.FirstOrDefault(symbol =>
            string.Equals(symbol.InstancePath, instancePath, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"ADS structure symbol '{instancePath}' was not found.");

    private async Task<T> ReadValueAsync<T>(string symbol, CancellationToken cancellationToken)
    {
        var result = await _client.ReadValueAsync<T>(symbol, cancellationToken);
        if (result.Failed)
            throw new AdsErrorException($"Could not read ADS symbol '{symbol}'.", result.ErrorCode);
        return result.Value!;
    }

    private async Task WriteValueAsync<T>(
        string symbol,
        T value,
        CancellationToken cancellationToken)
        where T : notnull
    {
        var result = await _client.WriteValueAsync(symbol, value, cancellationToken);
        if (result.Failed)
            throw new AdsErrorException($"Could not write ADS symbol '{symbol}'.", result.ErrorCode);
    }

    private async Task WriteIfChangedAsync<T>(
        string symbol,
        T original,
        T updated,
        CancellationToken cancellationToken)
        where T : notnull
    {
        if (EqualityComparer<T>.Default.Equals(original, updated))
            return;

        await WriteValueAsync(symbol, updated, cancellationToken);
    }
}
