namespace RackPeek.Domain.Persistence.Yaml;

using RackPeek.Domain.Resources;
using YamlDotNet.Core;

public interface IResourceYamlMigrationService
{
    Task<YamlRoot> DeserializeAsync(
        string yaml,
        Func<string, Task>? preMigrationAction = null,
        Func<YamlRoot, Task>? postMigrationAction = null);
}

public sealed class ResourceYamlMigrationService( 
    RackPeekConfigMigrationDeserializer deserializer)
    : IResourceYamlMigrationService
{
    private static readonly int CurrentSchemaVersion =
        RackPeekConfigMigrationDeserializer.ListOfMigrations.Count;

    public async Task<YamlRoot> DeserializeAsync(
        string yaml,
        Func<string, Task>? preMigrationAction = null,
        Func<YamlRoot, Task>? postMigrationAction = null)
    {
        if (string.IsNullOrWhiteSpace(yaml))
            return new YamlRoot();

        var version = deserializer.GetSchemaVersion(yaml);

        if (version > CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"Config schema version {version} is newer than this application supports ({CurrentSchemaVersion}).");
        }

        YamlRoot? root;

        if (version < CurrentSchemaVersion)
        {
            if (preMigrationAction != null)
                await preMigrationAction(yaml);

            root = await deserializer.Deserialize(yaml) ?? new YamlRoot();

            if (postMigrationAction != null)
                await postMigrationAction(root);
        }
        else
        {
            root = await deserializer.Deserialize(yaml);
        }

        return root ?? new YamlRoot();
    }
}