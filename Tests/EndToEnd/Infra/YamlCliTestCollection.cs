namespace Tests.EndToEnd.Infra;

[CollectionDefinition("Yaml CLI tests", DisableParallelization = true)]
public class YamlCliTestCollection
    : ICollectionFixture<TempYamlCliFixture>
{
}