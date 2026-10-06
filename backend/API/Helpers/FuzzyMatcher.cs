using System.Globalization;

namespace API.Helpers;

public static class FuzzyMatcher
{
    public static string Norm(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = s.Trim().ToLowerInvariant();

        // ukloni duple razmake
        while (s.Contains("  ", StringComparison.Ordinal))
            s = s.Replace("  ", " ", StringComparison.Ordinal);

        return s;
    }

    // 0..1 (1 = najbolji match)
    public static double Score(string query, string candidate)
    {
        query = Norm(query);
        candidate = Norm(candidate);

        if (query.Length == 0 || candidate.Length == 0) return 0;

        if (candidate.StartsWith(query, StringComparison.Ordinal))
            return 1.0;

        if (candidate.Contains(query, StringComparison.Ordinal))
            return 0.85;

        // fuzzy: Levenshtein similarity
        var dist = Levenshtein(query, candidate);
        var maxLen = Math.Max(query.Length, candidate.Length);
        if (maxLen == 0) return 0;

        var similarity = 1.0 - (double)dist / maxLen; // 0..1
        // malo “kazni” baš loše rezultate
        return 0.60 * Math.Max(0, similarity);
    }

    private static int Levenshtein(string a, string b)
    {
        // klasična DP implementacija
        var n = a.Length;
        var m = b.Length;

        if (n == 0) return m;
        if (m == 0) return n;

        var prev = new int[m + 1];
        var curr = new int[m + 1];

        for (int j = 0; j <= m; j++)
            prev[j] = j;

        for (int i = 1; i <= n; i++)
        {
            curr[0] = i;
            var ca = a[i - 1];

            for (int j = 1; j <= m; j++)
            {
                var cb = b[j - 1];
                var cost = (ca == cb) ? 0 : 1;

                curr[j] = Math.Min(
                    Math.Min(curr[j - 1] + 1, prev[j] + 1),
                    prev[j - 1] + cost
                );
            }

            (prev, curr) = (curr, prev);
        }

        return prev[m];
    }
}
