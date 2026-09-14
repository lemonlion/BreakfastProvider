using Kronikol;

namespace BreakfastProvider.Tests.Component.Shared;

/// <summary>
/// The cross-run history settings every component suite shares (Kronikol 3.9.0+). The ledger itself lives
/// on the orphan branch <c>kronikol-history</c>; CI fetches it before a run and folds each run's
/// <c>History.run.json</c> back into it afterwards (see <c>.github/workflows/_tests.yml</c> and the
/// <c>history</c> job of <c>ci-main.yml</c>). A pull request build reads against the branch it targets
/// on its own: Kronikol reads GITHUB_BASE_REF, so nothing here has to.
/// </summary>
public static class KronikolHistory
{
    /// <summary>
    /// One history stream per suite AND per lane. The same project runs in memory, against docker and
    /// against an external SUT under one CI run id, and the ledger would fold those three as shards of
    /// one run if they shared a suite name. CI sets <c>KRONIKOL_SUITE</c> to the lane (for example
    /// <c>xunit-in-docker</c>); a local run is its own suite. The suite is part of every scenario's
    /// stable id, so it is set here, in code, rather than guessed.
    /// </summary>
    public static ReportConfigurationOptions WithCrossRunHistory(this ReportConfigurationOptions options, string framework)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(framework);

        options.SuiteName = Environment.GetEnvironmentVariable("KRONIKOL_SUITE") is { Length: > 0 } suite
            ? suite
            : $"{framework.ToLowerInvariant()}-local";

        // Flakiness and duration verdicts need this many earlier runs; three rather than the default
        // five, so the published reports say something a few days sooner. The gate step in the workflow
        // is given the same bar (--min-runs 3), so the job summary and the report agree.
        options.HistoryMinRuns = 3;

        // The CTRF document sets `flaky` from the ledger, which is what fills the flaky list in the
        // github-test-reporter step of the workflow.
        options.GenerateCtrfReport = true;

        return options;
    }
}
