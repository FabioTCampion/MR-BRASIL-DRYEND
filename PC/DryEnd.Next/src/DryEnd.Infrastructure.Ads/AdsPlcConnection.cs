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
            await ReadValueAsync<int>($"{root}.tableID", cancellationToken),
            await ReadValueAsync<int>($"{root}.productionListNumber", cancellationToken),
            await ReadValueAsync<short>($"{root}.levelSelector", cancellationToken),
            await ReadValueAsync<string>($"{root}.fluteType", cancellationToken),
            await ReadValueAsync<float>($"{root}.lineSpeed", cancellationToken),
            await ReadValueAsync<bool>($"{root}.plcWatchDog", cancellationToken));
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
