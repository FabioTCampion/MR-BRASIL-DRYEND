namespace DryEnd.Infrastructure.Ads;

public sealed class AdsOptions
{
    public const string SectionName = "Ads";
    public const int PlcRuntimePort = 851;
    public const int SystemServicePort = 10000;

    public string AmsNetId { get; set; } = "192.168.30.79.1.1";
    public int Port { get; set; } = PlcRuntimePort;
    public int PollIntervalMilliseconds { get; set; } = 500;
    public int ReconnectDelayMilliseconds { get; set; } = 2_000;
    public string CurrentOrderRoot { get; set; } = ".currentOrder";
    public string NextOrderRoot { get; set; } = ".nextOrder";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AmsNetId))
            throw new InvalidOperationException("ADS AMS Net ID is required.");

        if (Port == SystemServicePort)
            throw new InvalidOperationException("TwinCAT system-service port 10000 is forbidden. Runtime state must never be controlled by this application.");

        if (Port != PlcRuntimePort)
            throw new InvalidOperationException($"Only PLC Runtime 1 port {PlcRuntimePort} is supported.");

        if (PollIntervalMilliseconds < 100)
            throw new InvalidOperationException("ADS polling interval must be at least 100 ms.");

        if (string.IsNullOrWhiteSpace(CurrentOrderRoot) || string.IsNullOrWhiteSpace(NextOrderRoot))
            throw new InvalidOperationException("ADS order symbol roots are required.");
    }
}
