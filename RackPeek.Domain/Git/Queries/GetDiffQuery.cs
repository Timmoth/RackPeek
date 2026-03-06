namespace RackPeek.Domain.Git.Queries;

public interface IGetDiffQuery
{
    string Execute();
}

public class GetDiffQuery(IGitRepository repo) : IGetDiffQuery
{
    public string Execute() => repo.IsAvailable ? repo.GetDiff() : string.Empty;
}
