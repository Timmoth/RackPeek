namespace RackPeek.Domain.Git.Queries;

public interface IGetStatusQuery
{
    GitRepoStatus Execute();
}

public class GetStatusQuery(IGitRepository repo) : IGetStatusQuery
{
    public GitRepoStatus Execute() => repo.GetStatus();
}
