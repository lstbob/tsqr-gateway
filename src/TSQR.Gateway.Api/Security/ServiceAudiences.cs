namespace TSQR.Gateway.Api.Security;

public static class ServiceAudiences
{
    public const string Gateway = "tsqr-gateway";
    public const string Autheo = "tsqr-autheo";
    public const string ToolLib = "tsqr-tool-lib";
    public const string Support = "tsqr-support";
    public const string Identity = "tsqr-identity";
    public const string SoupKitchen = "tsqr-soup-kitchen";
    public const string Communities = "tsqr-communities";
    public const string Learning = "tsqr-learning";

    /// <summary>
    /// Maps a YARP cluster id (<c>tool-lib</c>) to the audience its service
    /// expects (<c>tsqr-tool-lib</c>).
    /// </summary>
    public static string ForCluster(string clusterId) => $"tsqr-{clusterId}";
}
