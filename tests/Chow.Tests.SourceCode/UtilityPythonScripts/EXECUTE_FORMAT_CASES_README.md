# Formatting `CaseExecute` test cases

This folder contains [execute_format_cases.py](execute_format_cases.py), a small utility that converts a plain-text draft file into C# `CaseExecute` entries ready to paste into [ChowEngineTests.cs](../ChowEngineTests.cs).

## Requirements

- Python 3 (use `python` or `python3` depending on your system)

## Quick start

1. Create a draft input file (for example `my_cases.txt`).
2. Run the formatter and redirect stdout into your clipboard or a scratch file:

```powershell
python tests/Chow.Interpreter.Tests/UtilityPythonScripts/execute_format_cases.py my_cases.txt
```

From the `UtilityPythonScripts` directory:

```powershell
python execute_format_cases.py my_cases.txt
```

3. Copy the printed C# output into the appropriate `static readonly IReadOnlyList<CaseExecute> ...` array in `ChowEngineTests.cs`.

The script prints to **stdout** only. Warnings (such as a trailing case without `EXPECTED:`) go to **stderr**.

---

## Input file syntax

The input file is plain text. Structure it using three kinds of markers plus case blocks.

### Case block

Each test case is:

1. One or more lines of Chow source code
2. A line starting with `EXPECTED:` (case-insensitive) giving the value Chow should return

```text
1 + 2

EXPECTED: 3
```

Source is everything from the first non-blank code line up to (but not including) the `EXPECTED:` line.

### Region marker — `*`

A trimmed line starting with `*` (but not `**`) opens a C# `#region`:

```text
*Integer operands:
```

Output:

```csharp
        #region Integer operands:

        new(
            ...
        ),
```

- Starting a new `*` region closes the previous region (if any) and inserts a blank line between `#endregion` and the next `#region`.
- At end of file, an open region is closed with `#endregion`.

### Field marker — `**`

A trimmed line starting with `**` wraps subsequent cases in a `static readonly IReadOnlyList<CaseExecute>` field:

```text
**ExecuteArithmeticOperatorCases
```

Output:

```csharp
    static readonly IReadOnlyList<CaseExecute> ExecuteArithmeticOperatorCases =
    [
        ...
    ];
```

- The name after `**` becomes the C# field name (for example `**ExampleCases` → `ExampleCases`).
- Starting a new `**` field closes the previous field (`];`) and inserts a blank line before the next field declaration.
- At end of file, an open field is closed with `];` (and any open region inside it is closed first).

**Important:** `**` is checked before `*`, because `**` also begins with `*`.

### Regions inside fields

You can nest `*` regions inside a `**` field:

```text
**ExecuteFunctionScopeAndClosureCases

*Closures:

def make_closure():
    x = 5
    def closure():
        return x
    return closure

test_closure = make_closure()
test_closure()

EXPECTED: 5
```

---

## Blank lines

Blank lines are ignored **except** when they fall inside an active case (after the first source line, before `EXPECTED:`):

| Location | Behavior |
|----------|----------|
| Before the first source line of a case | Ignored |
| Between cases | Ignored |
| After the last source line, before `EXPECTED:` | Ignored (stripped) |
| Between source lines inside a case | **Kept** in the formatted output |

Use intentional blank lines inside source when the Chow program needs them (for example a blank line between two statements).

---

## `EXPECTED:` values

The text after `EXPECTED:` is converted to a C# expression for the second argument of `new(...)`:

| Input | Output |
|-------|--------|
| `EXPECTED: 3` | `new(3)` |
| `EXPECTED: 3.0` | `new(3.0)` |
| `EXPECTED: true` | `TrueChow` |
| `EXPECTED: false` | `FalseChow` |
| `EXPECTED: hello world` | `new("hello world")` |
| `EXPECTED: "hello world"` | `new("hello world")` |

`TrueChow` and `FalseChow` match the static fields already defined in `ChowEngineTests.cs`.

---

## Output shape

Each case is emitted as:

```csharp
        new(
            """
            <source lines, each indented>
            """,
            <expected expression>
        ),
```

- Field wrapper: 4-space indent (`static readonly ...` and `[` / `];`)
- Region / `new(` / `#endregion`: 8-space indent
- Source inside `"""`: 12-space indent

Consecutive cases are separated by a blank line. A blank line is also inserted after `#region` headers and between closed regions/fields as described above.

### Minimal example

**Input** (`example.txt`):

```text
**ExampleCases

*Sample:

a = 2

EXPECTED: 2
```

**Command:**

```powershell
python execute_format_cases.py example.txt
```

**Output:**

```csharp
    static readonly IReadOnlyList<CaseExecute> ExampleCases =
    [
        #region Sample:

        new(
            """
            a = 2
            """,
            new(2)
        ),

        #endregion
    ];
```

### Top-level regions (no field wrapper)

If you omit `**`, the script emits only regions and `new(...)` blocks—useful when pasting into an existing array:

```text
*Standalone smoke:

2 + 2

EXPECTED: 4
```

---

## Typical workflow

1. Draft cases in a `.txt` file using the syntax above.
2. Run `execute_format_cases.py` and review stdout.
3. Paste the output into `ChowEngineTests.cs`:
   - Whole `**` blocks → new or existing `IReadOnlyList<CaseExecute>` fields
   - Bare cases / `*` regions → inside an existing `[` ... `]` array
4. Add a `[TestCaseSource(nameof(YourFieldName))]` attribute on `Execute_ValidSourceCode_ReturnExpectedResult` if you created a new field list.
5. Build and run the interpreter tests.

---

## Troubleshooting

| Problem | Likely cause |
|---------|----------------|
| Case missing from output | No `EXPECTED:` line before the next marker or EOF |
| Warning on stderr about trailing case | Last case in the file has no `EXPECTED:` line |
| Blank line missing inside source | Blank line appeared before the first source line, or after the last source line (both are stripped) |
| `**` line treated as region | Should not happen if the line starts with `**`; check for typos |

---

## Related scripts

- [execute_cases_vs_python.py](execute_cases_vs_python.py) — reads formatted cases from `ChowEngineTests.cs` and compares each against Python 3 execution (informational; does not modify test files).
