# Kronikol history ledger

The cross-run ledger of BreakfastProvider's component test runs: an orphan data branch, appended to by the
history job of .github/workflows/ci-main.yml and never merged into main. Each lane of each CI run is one line.

Read it: git fetch origin kronikol-history && git show FETCH_HEAD:history.jsonl > history.jsonl
         kronikol query history <reports-dir> --history history.jsonl
