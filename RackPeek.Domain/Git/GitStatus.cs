namespace RackPeek.Domain.Git;

public enum GitRepoStatus
{
    NotAvailable,
    Clean,
    Dirty
}

public record GitLogEntry(string Hash, string Message, string Author, string Date);
public record GitSyncStatus(int Ahead, int Behind, bool HasRemote, string? Error = null);
