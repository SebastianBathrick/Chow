# Chow
Chow is a Pythonic scripting language for embedding in .NET projects, with an interpreter targeting .NET Standard 2.0 that has zero external dependencies. Currently, Chow is still in development, so features and implementations are subject to change, and use in production is not recommended until version 1.0.0+.

## Usecases
- Run familiar, Python-like code inside your .NET application in a safe sandboxed environment.
- Mix native .NET objects and functions with Chow code.
- Support scripting in .NET applications, allowing users to customize their applications using Chow (e.g., Unity/Godot games).

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
- Identity / membership: is, in, not in
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
- print, input, clear, len, abs, round, min, max, range, and type constructors int, float, str, bool, list, & dict.