# API

The `Chow` namespace exposes a small public API for embedding the interpreter in a host program. A host evaluates source code through `ChowEngine`, exchanging values with the interpreter as `ChowObject`s, and can optionally carry state across executions with a `ChowScope`. Every public type implements `IChowObject`, and the dedicated wrappers `ChowList`, `ChowDict`, and `ChowString` present Chow's `list`, `dict`, and `str` in a host-friendly form.

All of these types live in `[..\src\Chow\Api](../src/Chow/Api)`. Conversion between the public types and the interpreter's internal value representation happens behind the API surface, so the host only ever deals with the types described below.

**Documentation last updated:** 7-1-2026

## ChowEngine

Path: `[..\src\Chow\Api\ChowEngine.cs](../src/Chow/Api/ChowEngine.cs)`

`ChowEngine` is a static class and the entry point for executing Chow source code. The host hands it source code, optionally along with a scope to run in and a choice of whether the language's built-in functions are available, and receives the result of the execution back as a `ChowObject`. It is the only type a host needs to begin running Chow.

## IChowObject

Path: `[..\src\Chow\Api\IChowObject.cs](../src/Chow/Api/IChowObject.cs)`

`IChowObject` is the common interface shared by every public Chow object type. It lets the API accept and return Chow values without depending on any one concrete wrapper, and guarantees that every Chow value can render itself as a Chow-style string. It also carries the type-check flags—`IsNone`, `IsList`, `IsDictionary`, `IsScope`, `IsString`, `IsClass`, and `IsClassInstance`—so a host can discriminate a value it was handed without probing it and catching.

## ChowObject

Path: `[..\src\Chow\Api\ChowObject.cs](../src/Chow/Api/ChowObject.cs)`

`ChowObject` is the primary value type the host works with. It represents any single Chow value, such as an `int`, `float`, `str`, `bool`, `None`, `list`, `dict`, or `scope`, and is what `ChowEngine` returns and accepts.

It is designed to feel natural from host code: it converts to and from common .NET types, lets the host index into collections, read and assign attributes, and invoke methods on Chow objects, and can wrap host delegates so native functions can be called from Chow. It also supports value-based equality and comparison against ordinary .NET values.

For objects produced by a `class` declaration, `GetAttribute` and `SetAttribute` read and write instance fields and class variables, `Call` invokes a method by name, and `ClassName` reports which class the object belongs to—the declaring class for an instance, its own name for a class, and `null` for anything else. Attribute access on a type that has none raises a `RuntimeException`, the same error a host catches for any other Chow runtime failure.

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
