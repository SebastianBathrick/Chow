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
A Python sublanguage interpreter written entirely in C# for embedding in .NET projects. It targets .NET Standard 2.0 and has no external dependencies.

Chow is still in development, so features and the API are subject to change, and use in production is not recommended until version 1.0.0+.

## Use Cases
- Run safe, sandboxed Python code in your .NET application.
- Mix native .NET objects and functions with Chow code.
- Support scripting in .NET applications, allowing users to customize their applications using Chow (e.g., Unity/Godot games).
- Write frontend Python code for your Blazor applications.
- Add functionality to an application after it's already built using Chow scripts (even AOT-compiled applications).

## Features
The following is a list of features already implemented in Chow:

### API

- Inline Chow code execution.
- AOT compatibility.
- Interoperable .NET and Chow objects and variables.
- Sandboxed, managed Chow scopes (e.g., variables, functions, etc.).
- .NET delegate calls in Chow.

### Data Types

- int, float, bool, str, None
- list, dict, range
- functions (first-class objects)

### Literals

- Numbers: 42, 3.14
- Strings: "hello"
- F-strings: f"hi {name}"
- True, False, None
- List [1, 2, 3] and dict {"a": 1} literals
- Comments: # This is a comment

### Operators

- Arithmetic: +  -  *  /  //  %  **
- Comparison: ==  !=  <  <=  >  >=
- Logical: and  or  not (short-circuit)
- Identity/membership: is, in, not in
- Dict merge: |
- Unary: -x, not x

### Variables & Scope

- Assignment, function-level scoping (LEGB lookup)
- global and nonlocal declarations

### Control Flow

- if / elif / else
- while loops
- for ... in ... loops
- break, continue, pass

### Functions
- def with parameters and return
- Closures (capture enclosing variables live)
- First-class — pass and store functions as values

### Lists
- Indexing, negative indices, slicing (a[1:5:2]), assignment, iteration
- Methods: append, insert, pop, remove, reverse, clear
- Membership (in), structural equality, len()

### Dictionaries
- Key access/assignment, merge
- Methods: clear, copy, get, items, keys, pop, popitem, update, values

### Ranges
- range(stop), range(start, stop), range(start, stop, step)

### Built-in Functions
- print, input, clear, len, abs, round, min, max, range, and type constructors int, float, str, bool, list, and dict.

## Getting Started

Install via NuGet:

```
dotnet add package Chow
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

**Handle errors:**

```csharp
using Chow;

try
{
    ChowEngine.Run("x = 1 / 0");
}
catch (RuntimeException ex)
{
    Console.WriteLine(ex.Message); // ZeroDivisionError: division by zero on line 1
}
```
