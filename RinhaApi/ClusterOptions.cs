namespace RinhaApi;

public sealed class ClusterOptions
{
    public string? Ip { get; set; }
    public int? Port { get; set; }
    public string[]? Seeds { get; set; }
    public StartupMethod StartupMethod { get; set; } = StartupMethod.SeedNodes;
    public bool IsDocker { get; set; }
}

public enum StartupMethod
{
    SeedNodes,
    ConfigDiscovery,
    KubernetesDiscovery
}