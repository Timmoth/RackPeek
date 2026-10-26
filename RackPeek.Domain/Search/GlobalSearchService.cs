using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Search;

public record SearchResult(
    string Name,
    string Kind,
    string Url,
    string MatchedField,
    string MatchedValue,
    int Score
);

/// <summary>
/// Ranks a set of resources against a free-text query and returns the top N matches.
///
/// Scoring per resource: each searchable field is scored independently with a
/// weight (name &gt; ip &gt; tag &gt; label) and a shape modifier (equality &gt;
/// prefix &gt; substring &gt; subsequence). The resource takes the best-scoring
/// single field, so a strong name match always beats a weak label match.
/// </summary>
public static class GlobalSearchService {
    private const int _defaultMax = 8;

    private const int _weightName = 100;
    private const int _weightIp = 50;
    private const int _weightTag = 25;
    private const int _weightLabel = 10;

    public static IReadOnlyList<SearchResult> Search(
        IEnumerable<Resource> resources,
        string query,
        int max = _defaultMax) {
        if (string.IsNullOrWhiteSpace(query)) return [];

        var q = query.Trim().ToLowerInvariant();
        var results = new List<SearchResult>();

        foreach (Resource r in resources) {
            (string Field, string Value, int Score)? best = BestMatch(r, q);
            if (best is null) continue;

            results.Add(new SearchResult(
                Name: r.Name,
                Kind: r.Kind,
                Url: Resource.GetResourceUrl(r.Kind, r.Name),
                MatchedField: best.Value.Field,
                MatchedValue: best.Value.Value,
                Score: best.Value.Score));
        }

        return results
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .ToList();
    }

    private static (string Field, string Value, int Score)? BestMatch(Resource r, string q) {
        (string Field, string Value, int Score)? best = null;

        void Consider(string field, string? value, int weight) {
            if (string.IsNullOrEmpty(value)) return;
            var score = ScoreField(value, q, weight);
            if (score <= 0) return;
            if (best is null || score > best.Value.Score) {
                best = (field, value, score);
            }
        }

        Consider("name", r.Name, _weightName);

        var ip = GetIp(r);
        Consider("ip", ip, _weightIp);

        if (r.Tags is not null) {
            foreach (var tag in r.Tags) {
                Consider("tag", tag, _weightTag);
            }
        }

        if (r.Labels is not null) {
            foreach (KeyValuePair<string, string> kvp in r.Labels) {
                // Match against the value (label keys are usually category names,
                // values hold the meaningful data — IPs, hostnames, etc).
                Consider("label", $"{kvp.Key}: {kvp.Value}", _weightLabel);
            }
        }

        return best;
    }

    private static string? GetIp(Resource r) => r switch {
        SystemResource s => s.Ip,
        Service svc => svc.Network?.Ip,
        _ => null
    };

    private static int ScoreField(string? value, string lowerQuery, int weight) {
        if (string.IsNullOrEmpty(value)) return 0;

        var v = value.ToLowerInvariant();
        if (v == lowerQuery) return weight + 20;
        if (v.StartsWith(lowerQuery, StringComparison.Ordinal)) return weight + 10;
        if (v.Contains(lowerQuery, StringComparison.Ordinal)) return weight;
        return IsSubsequence(lowerQuery, v) ? weight - 10 : 0;
    }

    private static bool IsSubsequence(string needle, string haystack) {
        var i = 0;
        foreach (var c in haystack) {
            if (i < needle.Length && c == needle[i]) i++;
            if (i == needle.Length) return true;
        }
        return i == needle.Length;
    }
}
