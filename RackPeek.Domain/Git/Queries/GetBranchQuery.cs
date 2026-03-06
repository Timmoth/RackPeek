namespace RackPeek.Domain.Git.Queries;

public interface IGetBranchQuery
{
    string Execute();
}

public class GetBranchQuery(IGitRepository repo) : IGetBranchQuery
{
    public string Execute() => repo.IsAvailable ? repo.GetCurrentBranch() : string.Empty;
}
