namespace Tests.EndToEnd.Infra;

/// <summary>
///     Every test that registers RackPeek's services, directly or through the CLI host.
///     <para>
///         Registration writes process-wide statics as a side effect —
///         <c>RpkConstants.HasGitServices</c> among them — which is harmless in
///         production, where a process registers once, and a race in a test run, where
///         dozens of classes register with different configuration. Two classes in
///         different collections run in parallel, so a test asserting on one of those
///         statics can read a value another class set microseconds earlier.
///     </para>
///     <para>
///         One collection with parallelisation off is what makes those assertions mean
///         anything. The name is deliberately about the shared state rather than about
///         the YAML CLI, because membership is decided by "does this touch the statics",
///         not by what the test is nominally exercising.
///     </para>
/// </summary>
[CollectionDefinition("Process-wide static state", DisableParallelization = true)]
public class YamlCliTestCollection
    : ICollectionFixture<TempYamlCliFixture> {
}
