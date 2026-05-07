# Coding Conventions

Conventions empirically observed across `src/Chow/`. Follow these when adding or modifying code in this repo.

## File Layout

- **One type per file.** Filename matches the type name.
- **File-scoped namespaces** (`namespace Chow.X;`).
- **Using directives** at the top, ordered: `System` → `System.Collections.*` / `System.Text` → project namespaces (`Chow.*`).

## Naming

| Element | Convention | Example |
|---|---|---|
| Types (class/struct/enum) | `PascalCase` | `Scanner`, `TaggedUnion`, `OperationCode` |
| Public members (props, methods) | `PascalCase` | `BuildSyntaxTree()`, `LineNumber` |
| Private/internal fields | `_camelCase` (underscore prefix) | `_tokens`, `_chunk`, `_isDirty` |
| Parameters & locals | `camelCase` | `srcCode`, `leftOperand` |
| Constants | `UPPER_SNAKE_CASE` | `TAB_SIZE`, `DEFAULT_INT_VALUE` |
| Enum members | `PascalCase` | `OperationCode.Add`, `TokenType.LeftParenthesis` |

Methods are verb-prefixed when they perform actions (`ParseExpression`, `ScanToken`, `RegisterConstant`); predicates start with `Is`/`Check` (`IsDigitChar`, `Check`).

## Modifiers

- **Default access:** `internal`. Use `public` only on types/members consumed outside the assembly.
- **`sealed`:** apply to single-use classes (`Scanner`, `VirtualMachine`) and exception classes (`ParserException`, `ScannerException`).
- **`readonly struct`** for value types (`Token`, `Instruction`).
- **`static readonly`** for shared lookup tables (`Scanner._keywords`).
- **Expression-bodied members** for trivial getters and one-line methods (`public int Count => _operations.Count;`).

## Formatting

- **4-space indentation**, no tabs.
- **Braces on their own line** (Allman). Applies to types, methods, control flow.
- **All `if`/`while`/`switch` blocks use braces**, even single statements:
  ```csharp
  if (Check(TokenType.EndOfCode))
  {
      return new EmptyNode();
  }
  ```
- **`switch` cases:** label on its own line, body indented 4 spaces, terminated with `break`/`return`/`throw`. Use type-pattern matching for AST dispatch (`case EmptyNode _:`).

## Type Members Order

1. Constants
2. Static fields
3. Instance fields
4. Properties
5. Constructor(s)
6. Methods

## Single-Use Pattern

Classes that perform a one-shot transformation (`Scanner`, `Parser`, `Compiler`, `VirtualMachine`) guard re-entry on their public entry method:

```csharp
private bool _isDirty;

public Result DoWork()
{
    if (!_isDirty)
    {
        _isDirty = true;
    }
    else
    {
        throw new InvalidOperationException("This X instance can only be used once.");
    }
    // ...
}
```

These classes are also marked `sealed`.

## Constructors

Validate arguments at the top with throw expressions:

```csharp
_syntaxTreeRoot = syntaxTreeRoot ?? throw new ArgumentNullException(nameof(syntaxTreeRoot));

if (lineNumber < 1)
{
    throw new ArgumentOutOfRangeException(nameof(lineNumber));
}
```

Use `nameof(...)` in exception arguments.

## Exceptions

Custom exception classes:

- `sealed`, inherit from `Exception`.
- Constructor takes a `(string message, int lineNumber)` (or similar) and formats: `[line {lineNumber}] Error: {message}`.

Example: `ParserException`, `ScannerException`.

## Properties

- Prefer **auto-properties** with expression-bodied getters where possible.
- Use full property syntax with a backing field only when the setter needs validation (e.g. `TaggedUnion.IntegerValue`).
- Expose collections as `IReadOnlyList<T>` to hide mutability (`BlockNode.Statements`).

## Regions

Use `#region` only in large procedural classes (`Scanner`, `Compiler`, `Chunk`) to group related methods. Group names follow the pattern *"X Methods"* (e.g. `#region Statement Compilation`, `#region Constant Methods`). Small classes (Node types, value types) do not use regions.

## Documentation

- **XML doc comments** (`/// <summary>`) on public members of API surfaces (`Token`, `Chunk`, public node properties).
- Inline comments only where the *why* is non-obvious — operator precedence rules, suppression logic, etc. Avoid restating *what* the code does.

## Other Patterns

- **`ToString()` overrides** on every AST `Node`, `Token`, and `TaggedUnion` for diagnostics.
- **Static helpers** for repeated logic live as `private static` methods on the same class (`Compiler.GetExpressionOperationCode`, `ExpressionNode.IndentChildren`).
- **Type-switch dispatch** for AST lowering (see `Compiler.CompileNode`).
