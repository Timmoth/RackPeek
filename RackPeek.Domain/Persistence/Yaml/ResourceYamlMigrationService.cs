namespace RackPeek.Domain.Persistence.Yaml;

public interface IResourceYamlMigrationService {
    Task<YamlRoot> DeserializeAsync(
        string yaml,
        Func<string, Task>? preMigrationAction = null,
        Func<YamlRoot, Task>? postMigrationAction = null);
}

public sealed class ResourceYamlMigrationService(
    RackPeekConfigMigrationDeserializer deserializer)
    : IResourceYamlMigrationService {
    private static readonly int _currentSchemaVersion =
        RackPeekConfigMigrationDeserializer.ListOfMigrations.Count;

    public async Task<YamlRoot> DeserializeAsync(
        string yaml,
        Func<string, Task>? preMigrationAction = null,
        Func<YamlRoot, Task>? postMigrationAction = null) {
        if (string.IsNullOrWhiteSpace(yaml))
            return new YamlRoot();

        var version = deserializer.GetSchemaVersion(yaml);

        if (version > _currentSchemaVersion)
            throw new InvalidOperationException(
                $"Config schema version {version} is newer than this application supports ({_currentSchemaVersion}).");

        YamlRoot? root;

        if (version < _currentSchemaVersion) {
            if (preMigrationAction != null)
                await preMigrationAction(yaml);

            root = await deserializer.Deserialize(yaml) ?? new YamlRoot();

            if (postMigrationAction != null)
                await postMigrationAction(root);
        }
        else {
            root = await deserializer.Deserialize(yaml);
        }

        return root ?? new YamlRoot();
    }
}
