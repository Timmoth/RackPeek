using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RackPeek.Domain.Persistence.Yaml;
using RackPeek.Domain.Resources.UpsUnits;

namespace Tests.Yaml;

public class SchemaMigrationTests
{
    private static RackPeekConfigMigrationDeserializer Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var provider = services.BuildServiceProvider();

        return new RackPeekConfigMigrationDeserializer(
            provider,
            provider.GetRequiredService<ILogger<RackPeekConfigMigrationDeserializer>>());
    }


    [Fact]
    public async Task Migrations_ensures_version_exists()
    {
        // Arrange
        var sut = Setup();

        var yaml = """
                   resources:
                   - kind: Ups
                     name: example-ups
                   """;

        // Act
        var result = await sut.Deserialize(yaml);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(RackPeekConfigMigrationDeserializer.ListOfMigrations.Count, result.Version);
    }

    [Fact]
    public async Task Migrations_converts_runs_on_to_list()
    {
        // Arrange
        var sut = Setup();

        var yaml = """
                   version: 1
                   resources:
                   - kind: Ups
                     name: example-ups
                     runsOn: rack-a1
                   """;

        // Act
        var result = await sut.Deserialize(yaml);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Resources);
        var ups = Assert.IsType<Ups>(result.Resources[0]);
        Assert.Equal(ups.RunsOn, ["rack-a1"]);
    }
}
