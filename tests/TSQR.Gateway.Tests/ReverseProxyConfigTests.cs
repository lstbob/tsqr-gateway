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
    public void JwksRoute_IsPublicAndTargetsAutheo(string fileName)
    {
        var configuration = Load(fileName);
        var route = configuration.GetSection("ReverseProxy:Routes").GetChildren()
            .SingleOrDefault(candidate => candidate.Key == "autheo-jwks");

        Assert.NotNull(route);
        Assert.Equal("autheo", route["ClusterId"]);
        Assert.Equal("/.well-known/jwks.json", route["Match:Path"]);
        Assert.Null(route["AuthorizationPolicy"]);
    }

    [Fact]
    public void JwtAudience_IsGateway()
    {
        var configuration = Load("appsettings.json");

        Assert.Equal(
            TSQR.Gateway.Api.Security.ServiceAudiences.Gateway,
            configuration["Jwt:Audience"]);
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
    [InlineData("appsettings.json", "/api/soup-kitchen/dashboard/stats")]
    [InlineData("appsettings.json", "/api/soup-kitchen/events")]
    [InlineData("appsettings.json", "/api/soup-kitchen/events/1")]
    [InlineData("appsettings.json", "/api/soup-kitchen/meals")]
    [InlineData("appsettings.json", "/api/soup-kitchen/guests")]
    [InlineData("appsettings.json", "/api/soup-kitchen/donations")]
    [InlineData("appsettings.json", "/api/soup-kitchen/volunteers")]
    [InlineData("appsettings.Docker.json", "/api/soup-kitchen/dashboard/stats")]
    [InlineData("appsettings.Docker.json", "/api/soup-kitchen/events")]
    [InlineData("appsettings.Docker.json", "/api/soup-kitchen/events/1")]
    [InlineData("appsettings.Docker.json", "/api/soup-kitchen/meals")]
    [InlineData("appsettings.Docker.json", "/api/soup-kitchen/guests")]
    [InlineData("appsettings.Docker.json", "/api/soup-kitchen/donations")]
    [InlineData("appsettings.Docker.json", "/api/soup-kitchen/volunteers")]
    public void SoupKitchenReads_AreNotPublic(string fileName, string path)
    {
        var configuration = Load(fileName);
        var routes = configuration.GetSection("ReverseProxy:Routes").GetChildren().ToList();

        var publicRoute = routes.SingleOrDefault(route =>
            string.Equals(route["Match:Path"], path, StringComparison.Ordinal) &&
            !string.Equals(route["AuthorizationPolicy"], "Authenticated", StringComparison.Ordinal));

        Assert.Null(publicRoute);

        var catchAll = Assert.Single(routes, route => route.Key == "soup-kitchen-api");
        Assert.Equal("Authenticated", catchAll["AuthorizationPolicy"]);
        Assert.Equal("/api/soup-kitchen/{**catch-all}", catchAll["Match:Path"]);
    }

    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Docker.json")]
    public void LearningRoutes_StripExternalPrefix(string fileName)
    {
        var configuration = Load(fileName);
        var routes = configuration.GetSection("ReverseProxy:Routes").GetChildren()
            .Where(route => route["ClusterId"] == "learning")
            .ToList();

        Assert.NotEmpty(routes);

        foreach (var route in routes)
        {
            var path = route["Match:Path"];
            Assert.NotNull(path);
            Assert.StartsWith("/api/learning", path);

            var transforms = route.GetSection("Transforms").GetChildren().ToList();
            Assert.Equal(2, transforms.Count);
            Assert.Equal("/api/learning", transforms[0]["PathRemovePrefix"]);
            Assert.Equal("/api", transforms[1]["PathPrefix"]);
        }
    }

    [Theory]
    [InlineData("appsettings.json", "/api/learning/decks")]
    [InlineData("appsettings.json", "/api/learning/decks/1")]
    [InlineData("appsettings.json", "/api/learning/practice/queue")]
    [InlineData("appsettings.json", "/api/learning/stats")]
    [InlineData("appsettings.Docker.json", "/api/learning/decks")]
    [InlineData("appsettings.Docker.json", "/api/learning/decks/1")]
    [InlineData("appsettings.Docker.json", "/api/learning/practice/queue")]
    [InlineData("appsettings.Docker.json", "/api/learning/stats")]
    public void LearningReads_AreNotPublic(string fileName, string path)
    {
        var configuration = Load(fileName);
        var routes = configuration.GetSection("ReverseProxy:Routes").GetChildren().ToList();

        var publicRoute = routes.SingleOrDefault(route =>
            string.Equals(route["Match:Path"], path, StringComparison.Ordinal) &&
            !string.Equals(route["AuthorizationPolicy"], "Authenticated", StringComparison.Ordinal));

        Assert.Null(publicRoute);

        var catchAll = Assert.Single(routes, route => route.Key == "learning-api");
        Assert.Equal("Authenticated", catchAll["AuthorizationPolicy"]);
        Assert.Equal("/api/learning/{**catch-all}", catchAll["Match:Path"]);
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
