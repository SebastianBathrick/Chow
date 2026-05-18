# Chow
**Chow** is a mini-Python interpreter, written entirely in C#, available on platforms such as .NET, Mono, and Unity3D. It's an in-development library with many new features on the horizon, so stay tuned.

## Features
### Library:
- Easy-to-use API.
- Runtime bytecode compilation and execution
- No external dependencies, and targets Net Standard 2.0
- Runs on platforms that use ahead-of-time compilation (AOT).
- Interop data types, functions, and variables.
- Opt-in built-in functions, allowing modules to run in a sandboxed environment.

### Language:

- Data types: integers, floating-point numbers, strings, booleans, lists, dictionaries, range objects, functions, None, and host language objects.
- Expressions with Python operator precedence and chained comparisons.
- Python blocks.
- Python scope with global and nonlocal variables (LEGB).
- User-defined functions and closures.
- Dynamically typed variable declarations and assignments.
- Control structures: if / elif / else, while, and for loops
Iterables.
- Membership.
- List slicing.
- Expression statements.

### Upcoming Features

- pass statements.
- is operator.
- String slicing.
- in operator for strings.
- Tuple unpacking.
- In-language exceptions.
- Try/except, raise statements.
- Chow classes.

## Examples

### Console Application
```csharp
using Chow.Interpreter;

ChowModule module = new ChowModule();
string sourceCode =
"""
def fib(n):
    if n <= 1:
        return n
    return fib(n - 1) + fib(n - 2)

print(fib(10))
""";

module.Execute(sourceCode); // Output: 55
```
### Unity3D Application
```csharp
using System;
using UnityEngine;
using Chow.Interpreter;
using Chow.Interpreter.Values;

public class SimpleEnemyHealth : MonoBehaviour
{
    [SerializeField] double damageAmount = 10f;
    ChowModule _module;
    bool _isDefeated = false;

    void Start()
    {
        _module = new ChowModule();
        _module.SetBuiltInValue(BuiltInType.Print, new Action<ChowValue>(Debug.Log));
        
        string sourceCode =
            @"
            enemy_health = 100

            def damage_enemy(amount):
                global enemy_health
                enemy_health = enemy_health - amount
                if enemy_health <= 0:
                    print(""Enemy defeated!"")
                else:
                    print(f""Enemy health: {enemy_health}"")
                
            ";

        _module.Execute(sourceCode);
    }

    void Update()
    {
        // Damage the enemy if it hasn't been defeated & the player left clicks
        if (!_isDefeated && Input.GetKeyDown(KeyCode.Mouse0))
        {
            _module.Call("damage_enemy", damageAmount);
            _isDefeated = _module["enemy_health"].AsType<double>() <= 0;
        }
    }
}
 
```

# Package Development
As of writing, the latest [Chow NuGet package](https://www.nuget.org/packages/Chow/) version is `0.1.32`.

### Current Priorities
- More comprehensive codebase XML comments.
- Architecture Markdown documentation files.
- More comprehensive testing.
- More detailed exceptions (Chow language callstack, display whole line instead of just the number, etc.)
Call stack optimization.

### Dependencies
<details>
<summary><b>(Click to expand)</b></summary>

- ### Chow.Interpreter

  - Target framework: `netstandard2.0`


- ### Chow.Interpreter.Tests

  - Target framework: `net10.0`
  - Project reference: `Chow.Interpreter`
  - Packages:
    - [coverlet.collector](https://www.nuget.org/packages/coverlet.collector) v6.0.4
    - [Microsoft.NET.Test.Sdk](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk) v17.14.0
    - [NUnit](https://www.nuget.org/packages/NUnit) v4.3.2
    - [NUnit.Analyzers](https://www.nuget.org/packages/NUnit.Analyzers) v4.7.0
    - [NUnit3TestAdapter](https://www.nuget.org/packages/NUnit3TestAdapter) v5.0.0

- ### CodeEditor

    - Target framework: `net10.0-windows`
    - Project reference: `Chow.Interpreter`
    - Framework/UI: `Windows Forms`

</details>

## Quickstart

### Prerequisites
- .NET SDK 10 or later

### Restore
From the repository root:
```bash
dotnet restore Chow.slnx
```
### Build
```bash
dotnet build Chow.slnx
```
### Run Tests
Run the full test suite:
```bash
dotnet test Chow.slnx
```
Run interpreter tests only:
```bash
dotnet test tests/Chow.Interpreter.Tests/Chow.Interpreter.Tests.csproj
```
### Run the CLI
Start the Chow REPL:
```bash
dotnet run --project src/Chow/Chow.csproj
```
Run a .chw file:
```bash
dotnet run --project src/Chow/Chow.csproj -- path/to/file.chw
```
### Run the Windows Code Editor
On Windows:
```bash
dotnet run --project src/DeveloperTools/CodeEditor/CodeEditor.csproj
```
