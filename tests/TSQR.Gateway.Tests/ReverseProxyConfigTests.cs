using Microsoft.Extensions.Configuration;

namespace TSQR.Gateway.Tests;

public class ReverseProxyConfigTests
{
    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Docker.json")]
    public void Routes_ReferenceExistingClustersAndKnownPolicies(string fileName)
    {
        var configuration = Load(fileName);
        var routes = configuration.GetSection("ReverseProxy:Routes").GetChildren().ToList();
        var clusters = configuration.GetSection("ReverseProxy:Clusters").GetChildren()
            .Select(cluster => cluster.Key)
            .ToHashSet(StringComparer.Ordinal);
        var allowedPolicies = new HashSet<string>(StringComparer.Ordinal)
        {
            "Authenticated",
            "Anonymous",
        };

        Assert.NotEmpty(routes);
        Assert.NotEmpty(clusters);

        foreach (var route in routes)
        {
            var clusterId = route["ClusterId"];
            Assert.False(
                string.IsNullOrWhiteSpace(clusterId),
                $"Route '{route.Key}' has no ClusterId.");
            Assert.Contains(clusterId!, clusters);
            Assert.True(route.GetSection("Match").Exists(), $"Route '{route.Key}' has no Match section.");

            var policy = route["AuthorizationPolicy"];
            if (policy is not null)
                Assert.Contains(policy, allowedPolicies);
        }
    }

    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Docker.json")]
    public void SoupKitchenRoutes_StripExternalPrefix(string fileName)
    {
        var configuration = Load(fileName);
        var routes = configuration.GetSection("ReverseProxy:Routes").GetChildren()
            .Where(route => route["ClusterId"] == "soup-kitchen")
            .ToList();

        Assert.NotEmpty(routes);

        foreach (var route in routes)
        {
            var path = route["Match:Path"];
            Assert.NotNull(path);
            Assert.StartsWith("/api/soup-kitchen", path);

            var transforms = route.GetSection("Transforms").GetChildren().ToList();
            Assert.Equal(2, transforms.Count);
            Assert.Equal("/api/soup-kitchen", transforms[0]["PathRemovePrefix"]);
            Assert.Equal("/api", transforms[1]["PathPrefix"]);
        }
    }

    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Docker.json")]
    public void Clusters_HaveDestinations(string fileName)
    {
        var configuration = Load(fileName);
        var clusters = configuration.GetSection("ReverseProxy:Clusters").GetChildren().ToList();

        Assert.NotEmpty(clusters);

        foreach (var cluster in clusters)
        {
            var destinations = cluster.GetSection("Destinations").GetChildren().ToList();
            Assert.NotEmpty(destinations);
            foreach (var destination in destinations)
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(destination["Address"]),
                    $"Destination '{cluster.Key}/{destination.Key}' has no Address.");
            }
        }
    }

    private static IConfiguration Load(string fileName) =>
        new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(fileName, optional: false)
            .Build();
}
