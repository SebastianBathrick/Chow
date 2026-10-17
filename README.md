```
 .----------------.  .----------------.  .----------------.  .----------------. 
| .--------------. || .--------------. || .--------------. || .--------------. |
| |     _/\_/\   | || |  ____  ____  | || |     ____     | || | _____  _____ | |
| |   .' ___  |  | || | |_   ||   _| | || |   .'    `.   | || ||_   _||_   _|| |
| |  / .'   \_|  | || |   | |__| |   | || |  /  .--.  \  | || |  | | /\ | |  | |
| |  | |         | || |   |  __  |   | || |  | |    | |  | || |  | |/  \| |  | |
| |  \ `.___.'\  | || |  _| |  | |_  | || |  \  `--'  /  | || |  |   /\   |  | |
| |   `._____.'  | || | |____||____| | || |   `.____.'   | || |  |__/  \__|  | |
| |              | || |              | || |              | || |              | |
| '--------------' || '--------------' || '--------------' || '--------------' |
 '----------------'  '----------------'  '----------------'  '----------------' 
--------------------------------------------------------------------------------
                         LANGUAGE INTERPRETER FOR .NET     
--------------------------------------------------------------------------------
```

# Chow
A Python sublanguage interpreter written entirely in C# for embedding in .NET projects. It provides a sandboxed scripting environment featuring OOP, LEGB-enforced scope, closures, and more. Chow targets .NET Standard 2.0 and has no external dependencies.

## Use Cases
- Run safe, sandboxed Python code in your .NET application.
- Mix native .NET objects and functions with Chow code.
- Support scripting in .NET applications, allowing users to customize their applications using Chow (e.g., Unity/Godot games).
- Write frontend Python code for your Blazor applications.
- Add functionality to an application after it's already built using Chow scripts (even AOT-compiled applications).

## Features
The following is a list of features already implemented in Chow.

### API

- Inline Chow code execution.
- Compile once, run many times (precompiled `ChowProgram`s).
- AOT compatibility.
- Interoperable .NET and Chow objects and variables.
- Sandboxed, managed Chow scopes (e.g., variables, functions, etc.).
- .NET delegate calls in Chow.

### Data Types

- int, float, bool, str, None
- list, dict, set, frozenset, range
- dict views (keys(), values()) and iterators
- functions (first-class objects)
- classes and their instances

### Literals

- Numbers: 42, 3.14
- Strings: "hello"
- F-strings: f"hi {name}"
- True, False, None
- List [1, 2, 3], dict {"a": 1}, and set {1, 2} literals
- Comments: # This is a comment

### Operators

- Arithmetic: +  -  *  /  //  %  **
- Comparison: ==  !=  <  <=  >  >=
- Logical: and  or  not (short-circuit)
- Identity/membership: is, in, not in
- Bitwise on ints and bools: &  |  ^
- Sets: |  &  -  ^, and subset comparisons <  <=  >  >=
- Dict merge: |
- Unary: -x, not x

### Variables & Scope

- Assignment, function-level scoping (LEGB lookup)
- global and nonlocal declarations

### Control Flow

- if / elif / else
- while loops
- for ... in ... loops, with an optional else
- break, continue, pass

### Functions
- def with parameters and return
- Closures (capture enclosing variables live)
- First-class — pass and store functions as values

### Classes
- class declarations with an `__init__` constructor
- instance fields assigned through `self`
- methods (`self` is an explicit first parameter, as in Python)
- class-level variables, readable through the class or an instance
- Not supported: inheritance, `super()`, decorators such as `@classmethod`/`@staticmethod`, and
  dunder dispatch (`__str__`, `__eq__`, `__len__`)

### Lists
- Indexing, negative indices, slicing (a[1:5:2]), assignment, iteration
- Methods: append, insert, pop, remove, reverse, clear
- Membership (in), structural equality, len()

### Dictionaries
- Key access/assignment, membership (in), structural equality, len(), merge (|)
- Iteration in insertion order (for k in d, list(d), etc.)
- Methods: clear, get, keys, pop, setdefault, update, values
- keys() and values() return live views of the dict
- keys() views work like sets: | & - ^ with any iterable, comparisons with sets, and isdisjoint
- Changing a dict while iterating it raises RuntimeError exactly when CPython does

### Sets
- set and frozenset, with membership (in), equality between the two, len(), and iteration
- Operators: union |, intersection &, difference -, symmetric difference ^, and subset
  comparisons < <= > >=
- Methods: add, clear, copy, difference, difference_update, discard, intersection,
  intersection_update, isdisjoint, issubset, issuperset, pop, remove, symmetric_difference,
  symmetric_difference_update, union, update (frozenset has only the non-mutating ones)
- frozensets are hashable, so they can be set elements and dict keys
- Iteration order matches CPython run with PYTHONHASHSEED=0

### Ranges
- range(stop), range(start, stop), range(start, stop, step)

### Iterators
- iter(), next(), and reversed() return and advance lazy, single-use iterator objects
  (list_iterator, range_iterator, dict_keyiterator, etc.), as in Python
- Lists, ranges, strings, dicts, and dict views can all be reversed

### Built-in Functions
- print, input, clear, len, abs, round, min, max, range, iter, next, reversed, and type
  constructors int, float, str, bool, list, dict, set, and frozenset.

## Getting Started

Install via NuGet:

```
dotnet add package Chow --prerelease
```

**Run a snippet:**

```csharp
using Chow;

ChowObject result = ChowEngine.Run("1 + 2");
Console.WriteLine(result.ToString()); // 3
```

**Pass variables in via a scope:**

```csharp
using Chow;

var scope = new ChowScope();
scope["name"] = "world";

ChowEngine.Run("greeting = f'Hello, {name}!'", scope);

ChowObject greeting = scope["greeting"];
Console.WriteLine(greeting.ToString()); // Hello, world!
```

**Expose a .NET delegate to Chow code:**

```csharp
using Chow;

var scope = new ChowScope();
scope["greet"] = ChowObject.Create((object name) => $"Greetings {name}.");

ChowEngine.Run("print(greet(\"Linus\"))", scope); // Greetings Linus.
```

**Compile once, run many times:**

```csharp
using Chow;

ChowProgram program = ChowEngine.Compile("total = price * quantity");

foreach (var (price, quantity) in new[] { (3L, 4L), (10L, 2L) })
{
    var scope = new ChowScope();
    scope["price"] = price;
    scope["quantity"] = quantity;

    ChowEngine.Run(program, scope);
    Console.WriteLine(scope["total"].ToString()); // 12, then 20
}
```

**Configure a run:**

```csharp
using Chow;

var settings = new ChowRunSettings()
    .WithExcludeBuiltIns()          // leave out print, len, range, etc.
    .WithMaxRecursionDepth(5000);   // defaults to 1000; 0 means no limit

ChowEngine.Run("x = 1 + 2", new ChowScope(), settings);
```

Exceeding the recursion limit raises `RecursionError: maximum recursion depth of 5000 exceeded`. A scope's first run decides whether it has built-ins, so later runs on the same scope must pass the same `ExcludeBuiltIns` value.

**Handle errors:**

```csharp
using Chow;
using Chow.Interpreter.Exceptions;

try
{
    ChowEngine.Run("x = 1 / 0");
}
catch (CompileException ex)
{
    // The source code failed to compile, so none of it ran.
    Console.WriteLine(ex.Message);
}
catch (RuntimeException ex)
{
    Console.WriteLine(ex.Message); // ZeroDivisionError: division by zero on line 1
}
```

Both derive from `ChowException`, so catching it handles any Chow error. Each error also exposes its `ErrorName` (e.g. `ZeroDivisionError`) and `LineNumber`.
