# Entra-Flow

Entra-Flow is a user-provisioning pipeline. Drop a CSV of users into an input
folder, run the app, and it validates each record and splits the results into
**valid** and **rejected** output files — ready to feed downstream provisioning
(today as CSV; a SQL sink is planned).

## How it works

```
data/input/*.csv  ──▶  read ──▶ validate ──▶ write  ──▶  data/output/*-valid-<stamp>.csv
                                                          data/output/*-errors-<stamp>.csv
```

Each row is validated for:

- **Missing fields** — Name, Email, Department, or Role.
- **Duplicate email** — the second (and later) occurrence of an email within a
  file is rejected (case-insensitive).

Rejected rows are written to the `*-errors-*.csv` file with an `ErrorReasons`
column explaining why; valid rows are written to the `*-valid-*.csv` file.

## Prerequisites

- [.NET SDK 10.0](https://dotnet.microsoft.com/download) or later.

## Quick start

```bash
# 1. Put one or more CSV files in the input folder
#    (a sample is included at data/input/sample-users.csv)
cp my-users.csv data/input/

# 2. Run the pipeline
dotnet run --project src/EntraFlow.Cli

# 3. Find the results in data/output/
ls data/output/
```

Input CSVs must have the header row:

```csv
Name,Email,Department,Role
```

The exit code reflects the outcome: `0` = all valid, `2` = some rows rejected,
`1` = the run failed.

## Configuration

Settings live in [`src/EntraFlow.Cli/appsettings.json`](src/EntraFlow.Cli/appsettings.json)
under the `EntraFlow` section and can be overridden by environment variables or
command-line arguments (highest precedence last):

| Setting                 | Default       | Description                                              |
| ----------------------- | ------------- | -------------------------------------------------------- |
| `InputFolder`           | `data/input`  | Folder scanned for `*.csv` files to process.             |
| `OutputFolder`          | `data/output` | Where valid/error result files are written.              |
| `ArchiveProcessedFiles` | `false`       | Move processed inputs to `data/input/archive` when done. |

```bash
# Override via command line
dotnet run --project src/EntraFlow.Cli -- --EntraFlow:InputFolder=/data/incoming

# Override via environment variable
EntraFlow__ArchiveProcessedFiles=true dotnet run --project src/EntraFlow.Cli
```

## Project layout

```
EntraFlow.sln
├── src/
│   ├── EntraFlow.Core/        # Domain logic: models, validation, CSV IO, pipeline
│   └── EntraFlow.Cli/         # Console entry point (Generic Host + DI + config)
├── tests/
│   └── EntraFlow.Core.Tests/  # xUnit tests for the validator and CSV reader
└── data/
    ├── input/                 # Drop CSV files here (sample-users.csv included)
    └── output/                # Generated result files (git-ignored)
```

The pipeline writes results through the `IUserSink` interface, so a SQL-backed
sink (EF Core / Dapper, in a future `EntraFlow.Data` project) can be added
without changing the validation or pipeline code.

## Development

```bash
dotnet build      # build all projects
dotnet test       # run the test suite
```

CI runs build + tests on every push and pull request via
[GitHub Actions](.github/workflows/ci.yml).
