using System.Text.RegularExpressions;

namespace InMemoryEmulator.ClickHouse.Core;

/// <summary>
/// Translates the small ClickHouse dialect the emulator supports into DuckDB SQL. The rule list is
/// deliberately short and every rule is unit-tested; anything outside it throws
/// <see cref="NotSupportedException"/> naming the offending fragment — a loud failure in a component
/// test beats a silently wrong aggregate.
/// </summary>
public sealed partial class ClickHouseSqlTranslator(string database)
{
    private readonly string _database = database;

    [GeneratedRegex(@"^\s*CREATE\s+DATABASE\b", RegexOptions.IgnoreCase)]
    private static partial Regex CreateDatabaseRegex();

    [GeneratedRegex(@"^\s*CREATE\s+TABLE\b", RegexOptions.IgnoreCase)]
    private static partial Regex CreateTableRegex();

    [GeneratedRegex(@"^\s*DROP\s+TABLE\b", RegexOptions.IgnoreCase)]
    private static partial Regex DropTableRegex();

    [GeneratedRegex(@"^\s*(SELECT|WITH|SHOW|DESCRIBE|DESC|EXPLAIN)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ResultSetStatementRegex();

    [GeneratedRegex(@"^\s*(SELECT|WITH|INSERT|DELETE|SHOW)\b", RegexOptions.IgnoreCase)]
    private static partial Regex SupportedQueryRegex();

    // Statements ClickHouse understands but the emulator deliberately refuses (DuckDB would happily
    // run some of them, e.g. UPDATE, with semantics ClickHouse does not have).
    [GeneratedRegex(@"^\s*(UPDATE|ALTER|OPTIMIZE|TRUNCATE|RENAME|ATTACH|DETACH|SYSTEM|KILL|GRANT|REVOKE|SET|USE|EXCHANGE|CHECK|WATCH|EXISTS|DROP|CREATE)\b", RegexOptions.IgnoreCase)]
    private static partial Regex KnownUnsupportedStatementRegex();

    [GeneratedRegex(@"\bquantile\(\s*(?<p>[0-9]*\.?[0-9]+)\s*\)\s*\(\s*(?<x>[^()]+?)\s*\)")]
    private static partial Regex QuantileRegex();

    [GeneratedRegex(@"\bcount\(\s*\)")]
    private static partial Regex CountRegex();

    [GeneratedRegex(@"^\s*SELECT\s+1\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex SelectOneRegex();

    [GeneratedRegex(@"\bENGINE\s*=[\s\S]*$", RegexOptions.IgnoreCase)]
    private static partial Regex EngineClauseRegex();

    // ClickHouse-only constructs the emulator refuses rather than mistranslate.
    private static readonly (Regex Pattern, string Fragment)[] UnsupportedFragments =
    [
        (new Regex(@"\bFINAL\b", RegexOptions.IgnoreCase), "FINAL"),
        (new Regex(@"\bPREWHERE\b", RegexOptions.IgnoreCase), "PREWHERE"),
        (new Regex(@"\bSAMPLE\b", RegexOptions.IgnoreCase), "SAMPLE"),
        (new Regex(@"\bARRAY\s+JOIN\b", RegexOptions.IgnoreCase), "ARRAY JOIN"),
        (new Regex(@"\bWITH\s+TOTALS\b", RegexOptions.IgnoreCase), "WITH TOTALS"),
        (new Regex(@"\bLIMIT\s+\d+\s+BY\b", RegexOptions.IgnoreCase), "LIMIT n BY"),
        (new Regex(@"\bSETTINGS\b", RegexOptions.IgnoreCase), "SETTINGS"),
        (new Regex(@"\bALTER\s+TABLE\b", RegexOptions.IgnoreCase), "ALTER TABLE"),
        (new Regex(@"\bOPTIMIZE\s+TABLE\b", RegexOptions.IgnoreCase), "OPTIMIZE TABLE"),
        (new Regex(@"\bquantiles?[A-Za-z]+\("), "quantile* variants other than quantile(p)(x)"),
        (new Regex(@"\bquantiles\("), "quantiles()"),
        (new Regex(@"\bto[A-Z][A-Za-z0-9]*\("), "to*() conversion functions"),
        (new Regex(@"\bnow(64)?\(\)"), "now()"),
        (new Regex(@"\buniq[A-Za-z]*\("), "uniq*()"),
        (new Regex(@"\bgroupArray[A-Za-z]*\("), "groupArray*()"),
        (new Regex(@"\barg(Max|Min)\("), "argMax()/argMin()"),
    ];

    /// <summary>True for statements that produce a result set (SELECT, WITH, SHOW, DESCRIBE, EXPLAIN).</summary>
    public static bool ReturnsResultSet(string sql) => ResultSetStatementRegex().IsMatch(sql);

    /// <summary>True for DDL statements (CREATE DATABASE / CREATE TABLE / DROP TABLE).</summary>
    public static bool IsDdl(string sql) =>
        CreateDatabaseRegex().IsMatch(sql) || CreateTableRegex().IsMatch(sql) || DropTableRegex().IsMatch(sql);

    /// <summary>
    /// Translates one DDL statement. Returns <c>null</c> when the statement is a no-op for DuckDB
    /// (<c>CREATE DATABASE</c> — the emulator has exactly one database).
    /// </summary>
    public string? TranslateDdl(string statement)
    {
        var sql = TrimStatement(statement);

        if (CreateDatabaseRegex().IsMatch(sql))
            return null;

        if (CreateTableRegex().IsMatch(sql))
            return TranslateCreateTable(sql);

        if (DropTableRegex().IsMatch(sql))
            return StripDatabaseQualifier(sql);

        throw NotSupported(sql, sql.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries)[0]);
    }

    /// <summary>Translates a SELECT / INSERT / DELETE statement. <c>{name:Type}</c> placeholders are left for the binder.</summary>
    public string TranslateQuery(string statement)
    {
        var sql = TrimStatement(statement);

        if (IsDdl(sql))
            throw new InvalidOperationException($"'{sql}' is DDL — use {nameof(TranslateDdl)}.");

        if (!SupportedQueryRegex().IsMatch(sql))
        {
            var firstWord = sql.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? sql;
            if (KnownUnsupportedStatementRegex().IsMatch(sql))
                throw NotSupported(sql, firstWord);

            // Not a statement at all — the server reports SYNTAX_ERROR, so do we.
            throw ClickHouseEmulatorException.SyntaxError(
                $"Syntax error: failed at position 1 ('{firstWord}'): {sql}. Expected one of: SELECT, INSERT, DELETE, WITH, SHOW");
        }

        foreach (var (pattern, fragment) in UnsupportedFragments)
        {
            if (pattern.IsMatch(sql))
                throw NotSupported(sql, fragment);
        }

        if (SelectOneRegex().IsMatch(sql))
            return "SELECT CAST(1 AS UTINYINT) AS \"1\"";

        sql = StripDatabaseQualifier(sql);
        sql = QuantileRegex().Replace(sql, m => $"quantile_cont({m.Groups["x"].Value}, {m.Groups["p"].Value})");
        sql = CountRegex().Replace(sql, "CAST(count(*) AS UBIGINT)");

        return sql;
    }

    private string TranslateCreateTable(string sql)
    {
        var open = sql.IndexOf('(');
        if (open < 0)
            throw NotSupported(sql, "CREATE TABLE without a column list");

        var close = FindMatchingParen(sql, open);
        var header = StripDatabaseQualifier(sql[..open].Trim());
        var columnList = sql[(open + 1)..close];

        var columns = SplitTopLevel(columnList, ',')
            .Select(definition => TranslateColumnDefinition(definition, sql))
            .ToList();

        var tail = sql[(close + 1)..].Trim();
        tail = EngineClauseRegex().Replace(tail, string.Empty).Trim();
        if (tail.Length > 0)
            throw NotSupported(sql, tail);

        return $"{header} ({string.Join(", ", columns)})";
    }

    private string TranslateColumnDefinition(string definition, string statement)
    {
        var trimmed = definition.Trim();
        var split = trimmed.IndexOfAny([' ', '\t', '\r', '\n']);
        if (split < 0)
            throw NotSupported(statement, trimmed);

        var name = trimmed[..split];
        var type = trimmed[split..].Trim();

        // "name Type" only — DEFAULT / CODEC / COMMENT / TTL clauses are outside the supported dialect.
        if (type.IndexOfAny([' ', '\t', '\r', '\n']) >= 0)
            throw NotSupported(statement, trimmed);

        return $"{name} {ClickHouseTypeMap.DdlTypeToDuckDb(type)}";
    }

    private string StripDatabaseQualifier(string sql)
    {
        var qualifier = new Regex($@"\b{Regex.Escape(_database)}\.(?=[A-Za-z_`])");
        return qualifier.Replace(sql, string.Empty);
    }

    private static string TrimStatement(string statement) => statement.Trim().TrimEnd(';').Trim();

    private static NotSupportedException NotSupported(string sql, string fragment) =>
        new($"The in-memory ClickHouse emulator does not support '{fragment}' in: {sql}");

    private static int FindMatchingParen(string text, int openIndex)
    {
        var depth = 0;
        for (var i = openIndex; i < text.Length; i++)
        {
            if (text[i] == '(') depth++;
            else if (text[i] == ')' && --depth == 0) return i;
        }

        throw new NotSupportedException($"The in-memory ClickHouse emulator could not find the closing parenthesis in: {text}");
    }

    private static IEnumerable<string> SplitTopLevel(string text, char separator)
    {
        var depth = 0;
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '(') depth++;
            else if (text[i] == ')') depth--;
            else if (text[i] == separator && depth == 0)
            {
                yield return text[start..i];
                start = i + 1;
            }
        }

        var last = text[start..];
        if (last.Trim().Length > 0)
            yield return last;
    }
}
