# NuGet Grounding Content Evaluation Test Scenarios

The repository [kartheekp-ms/samples](https://github.com/kartheekp-ms/samples) contains test scenarios to evaluate GitHub Copilot performance **with and without grounding content**. Each project represents a task where LLM grounding content is hypothesized to improve Copilot's ability to reason about new or updated APIs.

## Success Criteria

For each scenario, grounding content effectiveness is measured by:
- **Accuracy**: Does Copilot complete the task correctly and all the tests pass?
- **Token Efficiency**: Fewer tokens used to complete the task
- **API time**: Reduced time/fewer API calls to reach a solution

Any improvement in these criteria signals that grounding content helps Copilot reason about package-specific behaviors.

## Test Scenarios

### 1. JsonSerializer.Core – Strict JsonSerializerOptions (.NET 10)

| Attribute | Value |
|-----------|-------|
| **Project** | `JsonSerializer.Core` |
| **Current State** | Thin wrapper around `System.Text.Json` using `JsonSerializerDefaults.General` with convenience methods for serialization/deserialization to strings and UTF-8 bytes. |
| **Prompt** | "Update this project to use Strict JsonSerializerOptions" |
| **Target Feature** | `JsonSerializerDefaults.Strict` – a new default mode added in .NET 10 |
| **Hypothesis** | Without grounding content, Copilot is unaware of `JsonSerializerDefaults.Strict` (introduced in .NET 10) and will require web search to discover it. With grounding content describing the new option, Copilot should immediately apply the correct API. |
| **Baseline Behavior** | Uses `JsonSerializerDefaults.General` in `JsonSerializerCore.cs:8` |

### 2. PipeJsonDemo – PipeReader Deserialization Overload (.NET 10)

| Attribute | Value |
|-----------|-------|
| **Project** | `PipeJsonDemo` |
| **Current State** | Deserializes JSON by converting `PipeReader` to a `Stream` via `reader.AsStream()`, then calling `JsonSerializer.DeserializeAsync<T>(stream, ...)`. |
| **Prompt** | "Update ReadJsonFromPipeAsync to use the new PipeReader overload for deserialization instead of converting to Stream" |
| **Target Feature** | `JsonSerializer.DeserializeAsync<T>(PipeReader, ...)` – a new overload added in .NET 10 |
| **Hypothesis** | Without grounding content, Copilot must rely on reflection or web search to discover the new `PipeReader` overload. With grounding content, Copilot can directly apply the optimized API without intermediate stream conversion. |
| **Baseline Behavior** | Uses `reader.AsStream()` + `DeserializeAsync<T>(stream, ...)` in `PipeJsonSerializer.cs:26-27` |

### 3. CommandLineDemo – Migrate to System.CommandLine (from McMaster.Extensions.CommandLineUtils)

| Attribute | Value |
|-----------|-------|
| **Project** | `CommandLineDemo` |
| **Current State** | CLI app using `McMaster.Extensions.CommandLineUtils 5.0.0` with `greet` and `sum` subcommands. Uses `CommandLineApplication`, `command.Argument()`, `command.Option()`, and `OnExecute()` patterns. |
| **Prompt** | "Migrate this project from McMaster.Extensions.CommandLineUtils to the latest version of System.CommandLine" |
| **Target Package** | `System.CommandLine 2.0.0` (stable GA release) |
| **Hypothesis** | Without grounding content, Copilot must rely on reflection or web search to understand System.CommandLine's current API (which has changed significantly from beta versions). With grounding content from the System.CommandLine package, Copilot can accurately migrate using `RootCommand`, `Command`, `Option<T>`, `Argument<T>`, `SetAction()`, and `ParseResult.GetValue()` patterns. |
| **Baseline Behavior** | Uses McMaster APIs in `Program.cs` with `CommandLineApplication`, `OnExecute()`, `command.Option<T>()` |

### 4. CommandLineLegacy – Migrate from System.CommandLine Beta to GA

| Attribute | Value |
|-----------|-------|
| **Project** | `CommandLineLegacy` |
| **Current State** | CLI app using `System.CommandLine 2.0.0-beta4.22272.1` with legacy patterns: `AddOption()`, `AddArgument()`, `SetHandler()`, `IConsole` for output redirection. |
| **Prompt** | "Migrate this project from System.CommandLine 2.0.0-beta4 to the latest stable version" |
| **Target Package** | `System.CommandLine 2.0.0` (stable GA release with breaking changes) |
| **Hypothesis** | The beta-to-GA migration involves significant breaking changes (e.g., `SetHandler()` → `SetAction()`, `IConsole` removal, `InvocationContext` changes). Without grounding content, Copilot relies on extensive web search to reason about API changes. With grounding content, Copilot can complete the migration faster and more accurately. |
| **Baseline Behavior** | Uses beta APIs: `AddOption()`, `SetHandler()`, `IConsole`, `context.ParseResult.GetValueForOption()` |

## Test Projects

Each scenario has a corresponding test project to validate correctness after migration:

| Test Project | Purpose |
|--------------|---------|
| **CommandLineDemo.Tests** | Validates `greet` and `sum` subcommand behavior |
| **CommandLineLegacy.Tests** | Validates `greet` and `sum` subcommand behavior |
| **JsonSerializer.Core.Tests** | Validates serialization/deserialization correctness |
| **PipeJsonDemo.Tests** | Validates async pipe-based JSON streaming |

## Evaluation Process

1. **Baseline Run (No Grounding)**: Execute each prompt without grounding content, measure tokens, API time, and accuracy.
2. **Grounded Run (With NuGet MCP Server enabled)**: Execute each prompt with package-specific grounding content delivered to Copilot via `get-package-context` tool in NuGet MCP Server, measure the same metrics.
3. **Compare**: Analyze whether grounding content improves accuracy, reduces tokens, or reduces API time.

### Tooling

Benchmarks will be executed using one of:
- [copilot-benchmark](https://github.com/dotnet-microsoft/ai-tools/tree/main/src/copilot-benchmark) tool
- GitHub Copilot CLI

## ⚠️ Important: Copilot Memory Considerations

During initial evaluations, the following behavior was observed:

- **First run without grounding content**: Copilot struggled to complete the task
- **Subsequent run with grounding content**: Copilot succeeded
- **Re-run without grounding content**: Copilot was able to accomplish the same task (via web search) where it previously failed

**Potential Cause**: GitHub Copilot Memory is enabled for our enterprise. Hence Copilot created memory for the [repository](https://github.com/kartheekp-ms/samples/settings/copilot/memory). This feature may cause Copilot to become more deterministic for similar tasks over time, even without grounding content, by learning from previous interactions.

**Recommended Test Protocol**:

To ensure accurate and reproducible results, clear all memory sources before each test iteration:

1. **Clear local memory**: Delete local Copilot memory/cache
2. **Clear repository memory**: Remove any stored memories for this repository in GitHub
3. **Fresh session**: Start each test in a new session to avoid cross-contamination

> **Note**: Without clearing memory between runs, it is difficult to isolate whether grounding content or accumulated memory is responsible for improved performance in subsequent attempts.

## 🔓 Open Questions

| Question | Status | Notes |
|----------|--------|-------|
| How to clear Copilot Memory between iterations? | **Open** | Manual deletion is possible but adds time to the evaluation process. Need an automated or streamlined approach. |