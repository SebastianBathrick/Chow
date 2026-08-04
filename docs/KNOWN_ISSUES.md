# Known Issues

Defects and rough edges found in Chow that have not been fixed yet. Every entry here was reproduced
against the library rather than inferred from reading the code; the snippets show actual observed
output.

Absent features are not listed as issues — see the feature list in
[..\README.md](../README.md) for what the language currently supports. The one exception is the
class limitations section at the end, which records deviations from Python that are easy to mistake
for bugs.

## Correctness

### A void .NET delegate crashes when called inside a loop

Calling a host-provided `Action` from Chow corrupts the value stack, so the second iteration of any
enclosing loop fails:

```csharp
var scope = new ChowScope();
scope["log"] = ChowObject.Create((Action<object>)(v => Console.WriteLine(v)));

ChowEngine.Run("""
    for i in [1, 2, 3]:
        log(i)
    """, scope);
```

```
1
Unhandled exception: InvalidOperationException: Stack empty.
```

`CallInteropFunction` in [..\src\Chow\Interpreter\VM\Processor.cs](../src/Chow/Interpreter/VM/Processor.cs)
invokes each `Action` shape but pushes no result, while every `Func` shape pushes one. A call
compiled as an expression statement is always followed by `PopExpressionStatementResult`, which pops
regardless. Outside a loop the stack is empty and that op quietly yields `None`, which is why the
problem is easy to miss. Inside a `for` loop the iterator is the top of the stack for the loop's
whole lifetime, so it gets popped instead — and the next `JumpOrForIteratorNext` finds nothing there.

Affects every void delegate shape: `Action`, `Action<object>` through `Action<object, object, object>`,
`Action<object[]>`, `Action<ChowObject>` through `Action<ChowObject, ChowObject, ChowObject>`, and
`Action<ChowObject[]>`.

**Fix:** push `SourceValue.None` in each `Action` branch, so that every call leaves exactly one value
behind. `Processor.CallValue` already compensates for the imbalance on the host-initiated path by
comparing stack depth before and after, so `ChowObject.Call` on a void callable is unaffected; only
calls made from Chow source are.

### Attribute errors on a function report the wrong type

```
AttributeError: 'str' object has no attribute 'nope'      // observed, on a function
AttributeError: 'function' object has no attribute 'nope' // expected
```

`SourceFunction.GetAttribute` in
[..\src\Chow\SourceData\SourceObjects\SourceFunction.cs](../src/Chow/SourceData/SourceObjects/SourceFunction.cs)
builds its `AttributeException` with a hard-coded `DataType.Str` where the slot wants the type of the
object that lacks the attribute. The method also guards that the attribute *name* is a string, and
the two were most likely conflated.

Only reachable through the host API. In Chow source, `f.bogus` is handled by
`Processor.ExecutePushAttribute`, which reads the receiver's own `DataType` and reports correctly.

**Fix:** pass `Type` — the class's own overridden `DataType` property — so it cannot drift again.

## Host API

### Type errors cannot be caught by anything narrower than `Exception`

`DataTypeException` in
[..\src\Chow\Interpreter\Exceptions\DataTypeException.cs](../src/Chow/Interpreter/Exceptions/DataTypeException.cs)
is internal *and* derives from `Exception` rather than `RuntimeException`. A host calling a method
with the wrong number of arguments therefore has no public type to catch:

```csharp
try { instance.Call("read", 1L); }
catch (RuntimeException) { }  // never runs — DataTypeException is not one
```

**Fix:** rebase it on `RuntimeException`. Its constructor would then gain the alias prefix and line
suffix that base applies, which changes its message text and breaks the `TypeError: …` assertions in
`ArithmeticEvaluatorTests`, `ComparisonEvaluatorTests`, and `LogicEvaluatorTests`. Worth doing
deliberately rather than as a rider on another change.

### Individual runtime error types are internal

`AttributeException`, `SubscriptException`, `UndefinedNameException`, and `ZeroDivisionException` all
derive from the public `RuntimeException` but are themselves internal. A host can catch "some Chow
runtime error" but cannot distinguish a missing attribute from a division by zero except by
inspecting the message.

More an API-surface decision than a defect, but it limits what a host can do about a failure.

### Indexing a class instance throws a raw .NET exception

```csharp
var x = instance["field"];   // NotSupportedException: GetItem
```

`ChowObject`'s indexer calls `GetItem`/`SetItem`, which fall through to the `SourceObject` base and
throw `NotSupportedException` — a .NET exception escaping the public API rather than a
`RuntimeException`. `GetAttribute` and `SetAttribute` convert this at the boundary; the indexer does
not.

Note that indexing is not the same as attribute access: `instance["field"]` is Python's
`__getitem__`, which Chow does not dispatch (see below). Use `GetAttribute("field")` to read a field.

**Fix:** convert `NotSupportedException` to `AttributeException` in the indexer, matching what the
attribute methods already do.

## Class limitations

These are deviations from Python semantics rather than defects, recorded because they are easy to
mistake for bugs.

### Dunder methods are not dispatched

Defining `__str__`, `__eq__`, `__len__`, and friends has no effect — they are stored as ordinary
methods and never consulted:

```python
class Pretty:
    def __str__(self):
        return "pretty!"

str(Pretty())    # <Pretty object>, not "pretty!"
```

`SourceValue.ToString` is a plain CLR call with no access to the `Processor`, so running a
user-defined `__str__` from there is not possible. Dispatch has to happen at the VM level instead,
where `CoerceToStr` and the `print` built-in are separate call sites.

### A value returned from `__init__` is silently discarded

```python
class Returner:
    def __init__(self):
        return 5      # CPython: TypeError: __init__() should return None
```

Chow constructs the instance and ignores the value. The construction override on the call frame
replaces whatever `__init__` returned, without checking that it was `None` first.

### A class variable always shadows a method of the same name

When a class body declares both a method and a class variable under one name, the variable wins
regardless of which came first in the source:

```python
class MethodFirst:
    def name(self):
        return "method"
    name = "variable"     # -> "variable"

class VariableFirst:
    name = "variable"
    def name(self):
        return "method"   # -> "variable"
```

`ClassDefinition.MakeClass` inserts the methods into the attribute table and then applies the class
variables over the top, so declaration order within the body is not preserved between the two kinds.
CPython would resolve each by source order.

### Inheritance is rejected rather than ignored

`class Dog(Animal):` raises a `SyntaxException` naming the unsupported feature. This is deliberate —
silently dropping the base class would be worse — but it means a base-class list is a parse error,
not a no-op.

## Tooling

### The CPython cross-check script reports false mismatches

`tests/Chow.Tests/UtilityPythonScripts/execute_cases_vs_python.py` re-runs each `CaseExecute` through
a real Python interpreter and compares results. It cannot resolve most of the C# expected-value
expressions in the current test file, so it reports a mismatch even when the two agree:

```
Expected (Chow): <unresolved: 7>
Python result:    7
NOT MATCHING
```

Roughly 250 of the cases are affected — the script predates the collection-expression `new(...)`
shorthand the test data now uses. Its Python-side results are still correct and useful; only the
comparison verdict is unreliable, so the output has to be read by eye.

The script is informational and does not fail the build.
