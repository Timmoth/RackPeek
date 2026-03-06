namespace RackPeek.Domain.Git.Queries;

public interface IGetLogQuery
{
    GitLogEntry[] Execute(int count = 20);
}

public class GetLogQuery(IGitRepository repo) : IGetLogQuery
{
    public GitLogEntry[] Execute(int count = 20) =>
        repo.IsAvailable ? repo.GetLog(count) : [];
}
