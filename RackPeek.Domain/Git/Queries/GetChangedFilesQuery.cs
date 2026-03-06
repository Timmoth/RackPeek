namespace RackPeek.Domain.Git.Queries;

public interface IGetChangedFilesQuery
{
    string[] Execute();
}

public class GetChangedFilesQuery(IGitRepository repo) : IGetChangedFilesQuery
{
    public string[] Execute() => repo.IsAvailable ? repo.GetChangedFiles() : [];
}
