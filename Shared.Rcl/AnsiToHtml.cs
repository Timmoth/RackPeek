using System.Text.RegularExpressions;

namespace Shared.Rcl;

public static class AnsiStripper {
    private static readonly Regex _csiRegex =
        new(@"\x1B\[[0-9;?]*[A-Za-z]", RegexOptions.Compiled);

    public static string Strip(string input) {
        if (string.IsNullOrEmpty(input))
            return "";

        return _csiRegex.Replace(input, "");
    }
}
