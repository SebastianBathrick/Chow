# `execute_cases_vs_python.py`

Compares `CaseExecute` tests in [ChowEngineTests.cs](../ChowEngineTests.cs) against Python 3. Informational only — mismatches are not failures.

## Run

```powershell
python tests/Chow.Interpreter.Tests/UtilityPythonScripts/execute_cases_vs_python.py
```

Optional path to a different `.cs` file:

```powershell
python execute_cases_vs_python.py path/to/ChowEngineTests.cs
```

Requires Python 3 on your PATH (`python` on Windows, `python3` on Linux/macOS). Override with the `PYTHON` environment variable.

## Output

Each case is printed in the same layout:

```
=== ListName #1 === [Optional Region]
Source:
<resolved Chow source>
Expected (Chow): <value>
Python result:    <value or error/skipped>
MATCHING
```

- **MATCHING** (green) — Python result equals the Chow expected value.
- **NOT MATCHING** (red) — values differ, Python errored, or the case could not be run.
- Colors appear in an interactive terminal; piped output is plain text.
- A dim separator line appears between cases in the terminal.

Skipped cases still appear in the report (for example when a C# constant referenced in the test is missing from the file).

## Chow vs Python semantics

Chow's API returns the value of the **final expression statement evaluated**, even inside blocks (`if`, loops, functions, etc.). Python only treats a **top-level** trailing expression as a result; expressions inside blocks are statements and produce no value.

Some cases will show **NOT MATCHING** for this reason — Chow has a value, Python shows `None`:

```
=== ControlFlowCases #4 === [If/Else-If/Else Statements]
Source:
if False:
    False
else:
    True
Expected (Chow): True
Python result:    None
NOT MATCHING
```

That difference is expected and does not mean the Chow test is wrong.

## Related

- [EXECUTE_FORMAT_CASES_README.md](EXECUTE_FORMAT_CASES_README.md) — create new cases with [execute_format_cases.py](execute_format_cases.py).
