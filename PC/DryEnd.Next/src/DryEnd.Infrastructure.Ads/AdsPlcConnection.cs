using DryEnd.Application;
using DryEnd.Domain;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.Ads.TypeSystem;

namespace DryEnd.Infrastructure.Ads;

public sealed class AdsPlcConnection : IPlcConnection
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

    public async Task<DiagnosticWriteResult> WriteCurrentFluteTypeAsync(string value, CancellationToken cancellationToken)
    {
        if (!_options.EnableDiagnosticWrites)
            throw new InvalidOperationException("Diagnostic ADS writes are disabled.");
        if (string.IsNullOrWhiteSpace(value) || value.Length > 80)
            throw new ArgumentOutOfRangeException(nameof(value), "Flute type must contain 1 to 80 characters.");
        if (!IsConnected)
            throw new InvalidOperationException("ADS client is not connected.");

        var symbol = $"{_options.CurrentOrderRoot}.fluteType";
        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            var previousValue = await ReadValueAsync<string>(symbol, cancellationToken);
            var writeResult = await _client.WriteValueAsync(symbol, value, cancellationToken);
            if (writeResult.Failed)
                throw new AdsErrorException($"Could not write ADS symbol '{symbol}'.", writeResult.ErrorCode);

            var confirmedValue = await ReadValueAsync<string>(symbol, cancellationToken);
            if (!string.Equals(value, confirmedValue, StringComparison.Ordinal))
                throw new InvalidOperationException($"ADS readback mismatch. Expected '{value}', received '{confirmedValue}'.");

            return new DiagnosticWriteResult(symbol, previousValue, confirmedValue, DateTimeOffset.UtcNow);
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
}

public sealed record DiagnosticWriteResult(
    string Symbol,
    string PreviousValue,
    string ConfirmedValue,
    DateTimeOffset ConfirmedAtUtc);
