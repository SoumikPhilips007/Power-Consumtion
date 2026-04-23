# Power Consumption Calculator

A WPF desktop application built with C# and .NET 8 for calculating power-consumption phases from:

- a power log file
- a devlog file
- operator-entered timestamps and requirement values

The application is structured with an MVVM-based UI, command-driven actions, and a calculation service that preserves the existing CSV export workflow.

## Requirements

- Windows 10 or 11
- .NET 8 SDK, or Visual Studio 2022 with .NET desktop workload

## Project Structure

- `MainWindow.xaml`: WPF view
- `ViewModels/`: UI state, validation, and commands
- `Services/`: file dialog integration and calculation/export logic
- `Infrastructure/`: MVVM support classes
- `FileOperation.cs`: file parsing and CSV helpers

## Run

### Visual Studio

1. Open `TimestampCalculator.sln`.
2. Set `TimestampCalculator` as the startup project if needed.
3. Run with `F5`.

### .NET CLI

```powershell
dotnet run --project TimestampCalculator.csproj
```

## Build

```powershell
dotnet build TimestampCalculator.csproj
```

## What It Does

- Accepts seven phase timestamps
- Accepts expected power values and a result title
- Supports manual inputs plus log-derived fallback timestamps
- Handles NG2250XP-specific logic through the checkbox option
- Reads power data from the selected log file
- Calculates phase durations and average power values
- Exports a CSV result file in the working directory

## Git Notes

The repository now includes:

- `.gitignore` for Visual Studio, WPF build output, and generated result files
- `.gitattributes` for stable text normalization

These settings keep local build artifacts and generated exports out of version control.
