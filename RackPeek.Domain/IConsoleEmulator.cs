namespace RackPeek.Domain;

public interface IConsoleEmulator {
    public Task<string> Execute(string input);
}
