using DryEnd.Application;
using DryEnd.Domain;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.Ads.TypeSystem;

namespace DryEnd.Infrastructure.Ads;

public sealed class AdsPlcConnection : IPlcConnection, IPlcOrderEditor, IPlcNextOrderWriter
{
    private readonly AdsClient _client = new();
    private readonly AdsOptions _options;
    private readonly SemaphoreSlim _operationLock = new(1, 1);

    public AdsPlcConnection(AdsOptions options)
    {
        options.Validate();
        _options = options;
    }

    public bool IsConnected => _client.IsConnected;

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (IsConnected)
            return;

        await _client.ConnectAsync(new AmsNetId(_options.AmsNetId), _options.Port, cancellationToken);
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

    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        _client.Disconnect();
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
        return new OrderSnapshot(
            await ReadValueAsync<string>($"{root}.startedAt", cancellationToken),
            await ReadValueAsync<int>($"{root}.tableID", cancellationToken),
            await ReadValueAsync<int>($"{root}.productionListNumber", cancellationToken),
            await ReadValueAsync<short>($"{root}.levelSelector", cancellationToken),
            await ReadValueAsync<string>($"{root}.paperComposition", cancellationToken),
            await ReadValueAsync<string>($"{root}.fluteType", cancellationToken),
            await ReadValueAsync<short>($"{root}.paperWidth", cancellationToken),
            await ReadStringListAsync(root, "paper", 5, cancellationToken),
            await ReadValueAsync<float>($"{root}.lineSpeed", cancellationToken),
            await ReadValueAsync<float>($"{root}.linearMeters", cancellationToken),
            await ReadValueAsync<float>($"{root}.linearMetersProduced", cancellationToken),
            await ReadValueAsync<float>($"{root}.linearMetersRemaining", cancellationToken),
            await ReadValueAsync<float>($"{root}.scorerHeightMM", cancellationToken),
            await ReadValueAsync<bool>($"{root}.plcWatchDog", cancellationToken),
            await ReadValueAsync<bool>($"{root}.aocRequest", cancellationToken),
            await ReadValueAsync<bool>($"{root}.changeOrderRequest", cancellationToken),
            await ReadValueAsync<bool>($"{root}.saveSQLFinished", cancellationToken),
            await ReadValueAsync<bool>($"{root}.saveSQLTimeOut", cancellationToken),
            await ReadValueAsync<bool>($"{root}.invertOrderLevel", cancellationToken),
            await ReadValueAsync<bool>($"{root}.invertOrderSide", cancellationToken),
            await ReadOrderChannelAsync($"{root}.order1", cancellationToken),
            await ReadOrderChannelAsync($"{root}.order2", cancellationToken),
            await ReadGeneratedOrderAsync($"{root}.generatedOrder", cancellationToken));
    }

    private async Task<OrderChannelSnapshot> ReadOrderChannelAsync(string root, CancellationToken cancellationToken)
    {
        var measures = new short[5];
        for (var index = 1; index <= measures.Length; index++)
            measures[index - 1] = await ReadValueAsync<short>($"{root}.sheetM{index}", cancellationToken);

        return new OrderChannelSnapshot(
            await ReadValueAsync<int>($"{root}.id", cancellationToken),
            await ReadValueAsync<string>($"{root}.product", cancellationToken),
            await ReadValueAsync<string>($"{root}.client", cancellationToken),
            await ReadValueAsync<short>($"{root}.sheetType", cancellationToken),
            await ReadValueAsync<short>($"{root}.sheetQuantity", cancellationToken),
            await ReadValueAsync<short>($"{root}.sheetLength", cancellationToken),
            measures,
            await ReadValueAsync<int>($"{root}.numberOfCuts", cancellationToken),
            await ReadValueAsync<int>($"{root}.numberOfCutsProduced", cancellationToken),
            await ReadValueAsync<int>($"{root}.numberOfCutsRemaining", cancellationToken),
            await ReadValueAsync<short>($"{root}.pileQuantity", cancellationToken),
            await ReadValueAsync<short>($"{root}.pileQuantityProduced", cancellationToken),
            await ReadValueAsync<short>($"{root}.pileQuantityRemaining", cancellationToken),
            await ReadValueAsync<short>($"{root}.pileCounter", cancellationToken),
            await ReadValueAsync<short>($"{root}.scrapCounter", cancellationToken));
    }

    private async Task<GeneratedOrderSnapshot> ReadGeneratedOrderAsync(string root, CancellationToken cancellationToken)
    {
        var knives = await ReadToolReferencesAsync(root, "knife", 10, cancellationToken);
        var scorers = await ReadToolReferencesAsync(root, "scorer", 20, cancellationToken);
        var knivesOutOfRange = await ReadActiveIndexesAsync($"{root}.statusWord.knifeOutOfRangeArr", 5, cancellationToken);
        var scorersOutOfRange = await ReadActiveIndexesAsync($"{root}.statusWord.scorerOutOfRangeArr", 8, cancellationToken);

        return new GeneratedOrderSnapshot(
            await ReadValueAsync<short>($"{root}.numberOfKnifes", cancellationToken),
            await ReadValueAsync<short>($"{root}.numberOfScorers", cancellationToken),
            await ReadValueAsync<short>($"{root}.numberOfSheets", cancellationToken),
            await ReadValueAsync<float>($"{root}.order1Width", cancellationToken),
            await ReadValueAsync<float>($"{root}.order2Width", cancellationToken),
            await ReadValueAsync<float>($"{root}.orderTotalWidth", cancellationToken),
            await ReadValueAsync<float>($"{root}.firstKnifePosition", cancellationToken),
            await ReadValueAsync<float>($"{root}.lastKnifePosition", cancellationToken),
            knives,
            scorers,
            await ReadValueAsync<bool>($"{root}.statusWord.orderNotOk", cancellationToken),
            knivesOutOfRange,
            scorersOutOfRange);
    }

    private async Task<IReadOnlyList<ToolReferenceSnapshot>> ReadToolReferencesAsync(
        string root,
        string toolName,
        int count,
        CancellationToken cancellationToken)
    {
        var result = new List<ToolReferenceSnapshot>(count);
        for (var index = 1; index <= count; index++)
        {
            var enabled = await ReadValueAsync<bool>($"{root}.{toolName}EnabledArr[{index}]", cancellationToken);
            var position = await ReadValueAsync<float>($"{root}.{toolName}PositionReferenceArr[{index}]", cancellationToken);
            result.Add(new ToolReferenceSnapshot(index, enabled, position));
        }
        return result;
    }

    private async Task<IReadOnlyList<int>> ReadActiveIndexesAsync(
        string root,
        int count,
        CancellationToken cancellationToken)
    {
        var result = new List<int>();
        for (var index = 1; index <= count; index++)
            if (await ReadValueAsync<bool>($"{root}[{index}]", cancellationToken))
                result.Add(index);
        return result;
    }

    private async Task<IReadOnlyList<string>> ReadStringListAsync(
        string root,
        string name,
        int count,
        CancellationToken cancellationToken)
    {
        var result = new string[count];
        for (var index = 1; index <= count; index++)
            result[index - 1] = await ReadValueAsync<string>($"{root}.{name}{index}", cancellationToken);
        return result;
    }

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
}
