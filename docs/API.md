# API

The `Chow` namespace exposes a small public API for embedding the interpreter in a host program. A host evaluates source code through `ChowEngine`, exchanging values with the interpreter as `ChowObject`s, and can optionally carry state across executions with a `ChowScope`. Source code that runs repeatedly can be compiled once into a `ChowProgram`. Every public type that represents a Chow value implements `IChowObject`, and the dedicated wrappers `ChowList`, `ChowDict`, and `ChowString` present Chow's `list`, `dict`, and `str` in a host-friendly form.

All of these types live in `[..\src\Chow\Api](../src/Chow/Api)`. Conversion between the public types and the interpreter's internal value representation happens behind the API surface, so the host only ever deals with the types described below.

**Documentation last updated:** 10-13-2026

## ChowEngine

Path: `[..\src\Chow\Api\ChowEngine.cs](../src/Chow/Api/ChowEngine.cs)`

`ChowEngine` is a static class and the entry point for executing Chow source code. The host hands it source code, optionally along with a scope to run in and a choice of whether the language's built-in functions are available, and receives the result of the execution back as a `ChowObject`. It is the only type a host needs to begin running Chow.

`Run` compiles the source code and executes it in one step. `Compile` performs only the first half, returning a `ChowProgram` that a second `Run` overload executes. Both `Run` overloads behave identically once the code is compiled. Source code is always compiled before the scope is touched, so code that fails to compile leaves the scope unchanged.

## ChowProgram

Path: `[..\src\Chow\Api\ChowProgram.cs](../src/Chow/Api/ChowProgram.cs)`

`ChowProgram` is a piece of Chow source code that has already been compiled, created by `ChowEngine.Compile`. A host that runs the same source code repeatedly, such as a script that runs on every frame or event, compiles it once and passes the program to `ChowEngine.Run` each time, skipping compilation on every run after the first.

A program is not tied to any scope: the same program can run against the same scope to build up state, or against different scopes to run independently. Running a program never changes it. It is safe to run one program from several threads at once, provided each concurrent run uses its own scope, since a scope is not safe to modify from multiple threads.

A program is an opaque handle with no public members. It is not a Chow value, so it does not implement `IChowObject` and cannot be stored in a scope or passed into Chow code.

## Errors

Path: `[..\src\Chow\Interpreter\Exceptions](../src/Chow/Interpreter/Exceptions)` (namespace `Chow.Interpreter.Exceptions`)

Chow reports errors in the source code it runs through two exception base types. `CompileException` is raised when source code cannot be compiled, such as a syntax error, an unterminated string, or an invalid `global`/`nonlocal` declaration. None of the source code has run when it is thrown, and its `LineNumber` reports the line the error occurred on. `ChowEngine.Compile` raises only compile errors, which lets a host validate source code before running it. `RuntimeException` is raised while the code is executing, such as for an undefined name or a division by zero.

## IChowObject

Path: `[..\src\Chow\Api\IChowObject.cs](../src/Chow/Api/IChowObject.cs)`

`IChowObject` is the common interface shared by every public Chow object type. It lets the API accept and return Chow values without depending on any one concrete wrapper, and guarantees that every Chow value can render itself as a Chow-style string. It also carries the type-check flags—`IsNone`, `IsList`, `IsDictionary`, `IsScope`, `IsString`, `IsClass`, and `IsClassInstance`—so a host can discriminate a value it was handed without probing it and catching.

## ChowObject

Path: `[..\src\Chow\Api\ChowObject.cs](../src/Chow/Api/ChowObject.cs)`

`ChowObject` is the primary value type the host works with. It represents any single Chow value, such as an `int`, `float`, `str`, `bool`, `None`, `list`, `dict`, or `scope`, and is what `ChowEngine` returns and accepts.

It is designed to feel natural from host code: it converts to and from common .NET types, lets the host index into collections, read and assign attributes, and invoke Chow objects, and can wrap host delegates so native functions can be called from Chow. It also supports value-based equality and comparison against ordinary .NET values.

There are two ways to invoke something. `CallMethod` calls a named method on this object, and `CallSelf` calls the object itself—useful when the callable is the value you are holding, such as a function read out of a scope. Calling a class constructs an instance of it, so `point.CallSelf(3, 4)` is the host spelling of `Point(3, 4)`. A callable that returns nothing yields the Chow `None` object.

Both come in two forms: one taking `ChowObject` arguments, and one taking native .NET values so an `int`, a `float`, or a `null` can be passed without wrapping it first. Arguments that are already Chow objects are unwrapped rather than wrapped again, so the two forms are interchangeable and can be mixed in one call.

Going the other way, a host delegate assigned into a scope becomes callable from Chow through an implicit conversion. Delegates come in the same two flavours: those taking `object` receive host values, while those taking `ChowObject` receive Chow values, which is what you want when the argument may be a `list`, a `dict`, or a class instance the delegate needs to inspect. Each flavour covers up to three fixed parameters plus a params-array form, with and without a return value, so any of them can be assigned into a scope directly. A delegate that returns nothing, or returns `null`, yields the Chow `None` object; one that is handed fewer arguments than it declares receives `None` for the rest rather than failing.

For objects produced by a `class` declaration, `GetAttribute` and `SetAttribute` read and write instance fields and class variables, `CallMethod` invokes a method by name, and `ClassName` reports which class the object belongs to—the declaring class for an instance, its own name for a class, and `null` for anything else. Attribute access on a type that has none raises a `RuntimeException`, the same error a host catches for any other Chow runtime failure.

## ChowScope

Path: `[..\src\Chow\Api\ChowScope.cs](../src/Chow/Api/ChowScope.cs)`

`ChowScope` represents a Chow scope: a collection of variable bindings. A host that wants state to persist across separate executions creates a scope and passes it to `ChowEngine`, so variables defined in one run remain available to the next. The host can also read and assign individual variables on the scope directly.

## ChowList

Path: `[..\src\Chow\Api\ChowList.cs](../src/Chow/Api/ChowList.cs)`

`ChowList` is a host-friendly wrapper over a Chow `list`, an ordered, mutable sequence of `ChowObject`s. It lets the host build up, inspect, and modify a list from .NET, mirroring the operations available on a list inside Chow.

## ChowDict

Path: `[..\src\Chow\Api\ChowDict.cs](../src/Chow/Api/ChowDict.cs)`

`ChowDict` is a host-friendly wrapper over a Chow `dict`, a mutable mapping of `ChowObject` keys to `ChowObject` values. It lets the host create, look up, and update entries from .NET, mirroring the operations available on a dictionary inside Chow.

## ChowString

Path: `[..\src\Chow\Api\ChowString.cs](../src/Chow/Api/ChowString.cs)`

`ChowString` is a host-friendly wrapper over a Chow `str`, an immutable sequence of characters. It offers read-only access to the string—its `Length` and individual characters by index—along with common string operations such as `Contains`, `StartsWith`, `EndsWith`, `IndexOf`, `Substring`, `ToUpper`, and `ToLower`. It converts implicitly to and from `ChowObject` and the host's own `string`.
