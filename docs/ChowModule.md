# ChowModule API Reference

`ChowModule` is the entry point for embedding the Chow interpreter in a C# application. It manages a persistent global scope across multiple `Execute` calls and provides methods for reading and writing globals, injecting host objects, and calling Chow functions from C#.

---

## Quick Start

```csharp
var module = new ChowModule();
module.Execute("x = 10");

var x = (long)module["x"]; // 10
```

---

## Executing Source Code

### `void Execute(string sourceCode)`

Compiles and runs a string of Chow source code. The global scope persists across calls, so variables and functions defined in one call are available in subsequent calls.

```csharp
module.Execute("x = 1");
module.Execute("y = x + 1");

var y = (long)module["y"]; // 2
```

`null`, empty, and whitespace-only strings are accepted and treated as no-ops — the scope is left unchanged.

Throws `ScannerException` or `ParserException` if the source contains a syntax error.

---

## Reading and Writing Global Variables

### Indexer — `object this[string name]`

**Get** returns the value of a global variable as a plain C# object (`long`, `double`, `bool`, `string`, or a `ChowValue` subclass for composite types).

```csharp
module.Execute("name = \"Alice\"");
var name = (string)module["name"]; // "Alice"
```

**Set** writes a value into the global scope, creating the variable if it does not already exist. This does not require a prior `Execute` call.

```csharp
module["multiplier"] = 3L;
module.Execute("result = multiplier * 10");
```

The setter accepts `long`, `double`, `bool`, `string`, `ChowValue` subclasses, and `ChowObject` instances.

---

### `ChowValue GetGlobal(string name)`

Same as the indexer getter, but returns a typed `ChowValue` rather than a plain `object`.

```csharp
ChowValue val = module.GetGlobal("x");
long n = val.AsType<long>();
```

Throws `InvalidOperationException` if `Execute` has not been called yet.  
Throws `GlobalAccessException` if the name is invalid or the variable does not exist.

---

### `void SetGlobal(string name, ChowValue value)`

Updates an existing global variable. Unlike the indexer setter, this method requires the variable to already exist and does not create new variables.

```csharp
module.Execute("count = 0");
module.SetGlobal("count", new ChowInt(5));
```

Throws `InvalidOperationException` if `Execute` has not been called yet.  
Throws `GlobalAccessException` if the name is invalid, the variable does not exist, or `value` is `null`.

---

### `bool ContainsGlobal(string name)`

Returns `true` if the named variable exists in the global scope, `false` otherwise. Safe to call before `Execute` — returns `false` when no scope exists yet.

```csharp
if (module.ContainsGlobal("result"))
{
    var result = module.GetGlobal("result");
}
```

---

## Calling Chow Functions from C#

### `ChowValue CallFunction(string functionName, params object[] arguments)`

Calls a Chow function that was defined during a previous `Execute` call. Arguments are passed as C# values and converted automatically (same accepted types as the indexer setter).

```csharp
module.Execute(@"
def add(a, b):
    return a + b
");

ChowValue result = module.CallFunction("add", 3L, 4L);
long sum = result.AsType<long>(); // 7
```

Returns the function's return value as a `ChowValue`. A Chow function that returns no value (or returns `None`) yields `ChowValue.None`.

Throws `InvalidOperationException` if `Execute` has not been called yet.  
Throws `GlobalAccessException` if the name is invalid or the function does not exist.

---

## Injecting Host Objects

### `ChowObject`

`ChowObject` is a host-defined object that can be injected into the Chow scope and accessed from Chow source by attribute access (`obj.attr`).

```csharp
var player = new ChowObject("Player");
player["health"] = 100L;
player["name"] = "Hero";

module["player"] = player;
module.Execute("player.health = player.health - 10");

long health = (long)player["health"]; // 90
```

**`ChowObject(string className)`** — constructs a new object with the given class name. The name is used for display purposes only (`<ClassName object>`).

**`object this[string name]`** — get or set an attribute by name.

**`ChowValue GetAttribute(string name)`** — get an attribute as a typed `ChowValue`.

**`bool ContainsAttribute(string name)`** — returns `true` if the attribute exists.

**`string ClassName`** — the class name provided at construction.

Attribute names follow the same rules as [global variable names](#global-variable-name-rules).

---

## Built-in Functions

The 14 standard built-ins (`print`, `input`, `float`, `str`, `int`, `bool`, `list`, `dict`, `len`, `type`, `abs`, `round`, `min`, `max`) are seeded into the global scope automatically when a `ChowModule` is constructed — no setup call is required.

Hosts can disable, re-enable, or override individual built-ins via the methods below. Each built-in is identified by a `BuiltInType` enum value (e.g. `BuiltInType.Print`) so client code doesn't depend on the source-language name string.

### `void SetBuiltInActive(BuiltInType type, bool active)`

Controls whether a built-in is visible to Chow source code. Disabling removes the binding from the global scope; a reference to it from Chow code then raises a `NameError`. Re-enabling reinstalls the currently *configured* value (the default, or whatever was most recently passed to `SetBuiltInValue`). Idempotent.

```csharp
module.SetBuiltInActive(BuiltInType.Print, false);
module.Execute("print(\"hi\")"); // throws UndefinedNameException
```

### `void SetBuiltInValue(BuiltInType type, object value)`

Overrides the implementation of a built-in. The override is retained across `SetBuiltInActive` toggles, so hosts don't have to re-apply it after a disable/enable cycle. If the built-in is currently active, the new value takes effect immediately; if inactive, the override is held until the next `SetBuiltInActive(type, true)` call.

Accepted value types match those of the [indexer setter](#global-variable-access) (delegates such as `Func<ChowValue, ChowValue>`, `ChowValue` subclasses, `ChowObject`, primitives). `null` throws `ArgumentNullException` — use `SetBuiltInActive(type, false)` to remove.

```csharp
module.SetBuiltInValue(BuiltInType.Print, (Func<ChowValue, ChowValue>)(arg =>
{
    Debug.Log($"chow: {arg}");
    return ChowValue.None;
}));
module.Execute("print(\"hi\")"); // routes to Debug.Log
```

### `bool IsBuiltInActive(BuiltInType type)`

Returns `true` if the built-in is currently callable from Chow source code.

---

## ChowValue Type System

All values returned from the API are `ChowValue` instances. Use `AsType<T>()` to extract the underlying value, or pattern-match on the concrete type.

| Chow type | C# class | `AsType<T>()` support |
|---|---|---|
| Integer | `ChowInt` | `long`, `double` (widening), `bool` (non-zero) |
| Float | `ChowFloat` | `double`, `long` (truncated), `bool` (non-zero) |
| Boolean | `ChowBool` | `bool`, `long` (0 or 1), `double` (0.0 or 1.0) |
| String | `ChowStr` | `bool` (non-empty); use `.Value` for the `string` |
| List | `ChowList` | `bool` (non-empty); use `Count` and `this[int]` |
| Dict | `ChowDict` | `bool` (non-empty); use `Count` and `this[ChowValue]` |
| Object | `ChowObject` | `bool` (always `true`) |
| None | `ChowNone` | no supported conversions — check `value.IsNone` first |
| Function | `ChowFunction` | no supported conversions |
| Dynamic | `ChowDynamic` | `T` if the wrapped value is assignable to `T` |

`AsType<T>()` throws `Chow.Interpreter.Values.InvalidCastException` for unsupported conversions.

**`bool IsNone`** — `true` if the value is `None`. Equivalent to `value == ChowValue.None`.

**`ChowValue.None`** — the singleton `None` value.

```csharp
ChowValue result = module.CallFunction("maybe_returns_none");

if (result.IsNone)
{
    // function returned None
}
else if (result is ChowStr str)
{
    Console.WriteLine(str.Value);
}
else
{
    long n = result.AsType<long>();
}
```

---

## Global Variable Name Rules

A valid global variable name must:

- Contain only ASCII letters (`A–Z`, `a–z`), digits (`0–9`), or underscores (`_`)
- Start with a letter or underscore (not a digit)
- Not be a reserved keyword

Reserved keywords: `True`, `False`, `None`, `and`, `or`, `not`, `is`, `in`, `def`, `return`, `class`, `with`, `as`, `global`, `if`, `else`, `elif`, `for`, `while`, `break`, `continue`, `pass`, `try`, `except`, `finally`, `raise`, `assert`

---

## Exceptions

| Exception | When thrown |
|---|---|
| `GlobalAccessException` | Name is invalid, reserved, undefined, or `null` value passed to `SetGlobal` |
| `InvalidOperationException` | A read API (`GetGlobal`, indexer get, `CallFunction`) is called before `Execute` has been called |
| `ScannerException` | Source code contains a lexical error |
| `ParserException` | Source code contains a syntax error |
| `ChowRuntimeException` | A runtime error occurs during `Execute` or `CallFunction` (e.g. undefined name, type mismatch, missing attribute) |
