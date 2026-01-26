namespace Tests.EndToEnd;

public sealed class TempYamlCliFixture : IAsyncLifetime
{
    public string Root { get; } = Path.Combine(
        Path.GetTempPath(),
        "rackpeek-tests",
        Guid.NewGuid().ToString()
    );

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(Root);

        // Create empty YAML files so repo loads cleanly
        foreach (var file in new[]
                 {
                     "servers.yaml",
                     "aps.yaml",
                     "desktops.yaml",
                     "switches.yaml",
                     "ups.yaml",
                     "firewalls.yaml",
                     "laptops.yaml",
                     "routers.yaml"
                 }) 
        {
            File.WriteAllText(Path.Combine(Root, file), "");
        }

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Directory.Delete(Root, recursive: true);
        return Task.CompletedTask;
    }
}