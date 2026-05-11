# Chow

## Current Features
- Pythonic Syntax
- REPL csproj
- Floating-point data type
- Integer data type
- Boolean data type
- String data type
- Function data type (defined in source code)
- Object data type (allows for interop objects)
- Arithmetic Expressions
- Comparison Expressions
- Logic Expressions
- Nested expressions
- Top-level statements
- Expression statements
- Variable declaration statements
- Variable assignment statements
- If, elif, and else statements
- Interop variable/function API
- Callable interop/non-interop functions
- Closures (functions can be declared in any block)


## Tests
390/390 passing tests

## Small TODOs
- Get rid of "is dirty" flags in major classes (redundant)
- Remove top-level return statements (not needed)
- Add ChowSyntaxErrorException and use it for top-level return statements
- Add float literals starting with decimals
- Have the output on the REPL start at cursor positon 0 instead of 3
- Ensure negated primaries starting with "-" and "not" are flagged as such

## Behavior to Investigate
REPL output:
```python
>>> 1 == True
    False
>>> 0 == False
    False
>>> 1 == 1 == True
    False
```
Expected output:
```python
>>> 1 == True
    True
>>> 0 == False
    True
>>> 1 == 1 == True
    True
```

# Language
## Literals
- **Integer:** `0`, `-2`, `3`
- **Float:** `0.0`, `-2.0`, `3.0`, `4.5`, `81.`
- **Bool:** `True`, `False`
- **String:** `"Hello"`, `"World"`, `"123"`, `'abc'`
- **None:** `None`
- **List:** `[1, 2, 3]`, `[]`

## Operators
- **Arithmetic:** `+`, `-`, `*`, `/`, `//`, `%`, `**`
- **Comparison:** `==`, `!=`, `<`, `>`, `<=`, `>=`
- **Logical:** `and`, `or`, `not`
- **Unary:** `-x`, `not x`
- **Assignment:** `=`
- **Attribute access:** `obj.name`
- **Subscript:** `seq[i]`, `seq[start:stop:step]`
- **Call:** `f(args)`

## Comments
```python
# This is a comment!
print("Hello World") # Another comment!
```

## Variable Declarations & Assignments
```python
# The variable is an int based on the expression assigned
myVar = 2

# Changes type to a string, because variables are dynamically typed
myVar = "String expression"
```

## If Statements
```python
print("Please enter an integer: ")

x = int(input())

if x < 0:
    x = 0
    print('Negative changed to zero')
elif x == 0:
    print('Zero')
elif x == 1:
    print('Single')
else:
    print('More')
```
## Functions
```python
def select_int():
    print("Please enter an integer: ")
    return int(input())

def eval_input_int(value):
    if value < 0:
        print("Your number is negative")
    else:
        print("Your number is positive")

selection = select_int()
eval_input_int(selection)

x = eval_input_int

# Outputs the same as eval_input_int call above
x(selection) 
```

### Closures
```python
def make_adder(x):
    def add(y):
        return x + y
    return add

add5 = make_adder(5)
print(add5(3))      # 8
print(add5(10))     # 15

def outer_value():
    x = 99

    def inner():
        return x

    return inner

f = outer_value()
print(f())          # 99
```

## Expression Statements
```python
# The expression will be evaluated and ignored
# To access the result of an evaluated expression, create a child class of `IExprStatementHook`.
9 + 10
```

## Lists

Lists hold an ordered, mutable sequence of values. Elements can be any type, including other lists.

### Literals
```python
empty = []
nums = [1, 2, 3]
mixed = [1, "two", 3.0, None, True]
nested = [[1, 2], [3, 4]]
```

### Indexing
Indices are zero-based. Negative indices wrap from the end.
```python
a = [10, 20, 30]

a[0]   # 10
a[-1]  # 30
a[2]   # 30

a[0] = 99
a[-1] = 0
# a is now [99, 20, 0]
```

### Slicing
```python
a = [0, 1, 2, 3, 4]

a[1:4]    # [1, 2, 3]
a[:3]     # [0, 1, 2]
a[2:]     # [2, 3, 4]
a[:]      # [0, 1, 2, 3, 4]   (copy)
a[::2]    # [0, 2, 4]
a[::-1]   # [4, 3, 2, 1, 0]   (reversed)
a[1:5:2]  # [1, 3]
```

Slicing always returns a new list. A step of `0` raises an error. Slice assignment (`a[1:3] = [9, 9]`) is not yet supported.

### Methods
```python
a = [1, 2, 3]

a.append(4)   # a is [1, 2, 3, 4]
a.insert(0, 0)  # a is [0, 1, 2, 3, 4]
a.pop()       # returns 4; a is [0, 1, 2, 3]
a.pop(0)      # returns 0; a is [1, 2, 3]
a.remove(2)   # a is [1, 3]
a.reverse()   # a is [3, 1]
a.clear()     # a is []
```

Methods are first-class values — they can be stored in variables and called later. The method stays bound to the original list:
```python
a = [1]
f = a.append
f(2)
f(3)
# a is [1, 2, 3]
```

Accessing an attribute that isn't a list method raises an `AttributeError`:
```python
[1].fake
# AttributeError: 'list' object has no attribute 'fake' on line 1
```

### Operators
```python
[1, 2] + [3, 4]   # [1, 2, 3, 4]   concatenation
[0] * 3           # [0, 0, 0]      repetition
3 * [0]           # [0, 0, 0]
[0] * -1          # []             non-positive count yields empty

[1, 2] == [1, 2]  # True   element-wise equality
[1, 2] == [1, 3]  # False
[1, [2]] == [1, [2]]  # True   recursive
```

### Truthiness
Empty lists are falsy, non-empty are truthy.
```python
if []:
    "never"
else:
    "empty is falsy"
```

# Library API

Host C# code exposes variables and callable functions to Chow source through `ChowModule`. Variables flow in and out as `ChowValue` instances; functions are wrapped in `ChowDynamic`.

## Exposing variables
```csharp
ChowModule module = new ChowModule();

// Host -> Chow
module["greeting"] = new ChowStr("Hello");
module["count"] = new ChowInt(42);

// Chow -> Host (after Execute)
module.Execute("result = count * 2");
int doubled = module["result"].As<int>();
```

Reading an unset name throws `ChowApiNameErrorException`.

## Exposing functions
Wrap a delegate in `ChowDynamic`. Supported signatures:

| Delegate                              | Behavior                          |
|---------------------------------------|-----------------------------------|
| `Func<ChowValue>`                     | Zero-arg, returns a value         |
| `Func<ChowValue, ChowValue>`          | One-arg, returns a value          |
| `Func<ChowValue[], ChowValue>`        | Variadic, returns a value         |
| `Action`                              | Zero-arg, returns `None`          |
| `Action<ChowValue>`                   | One-arg, returns `None`           |
| `Action<ChowValue[]>`                 | Variadic, returns `None`          |

```csharp
// Zero args
module["pi"] = new ChowDynamic(() => new ChowFloat(3.14159f));

// One arg
module["double"] = new ChowDynamic((ChowValue v) =>
    new ChowInt(v.As<int>() * 2));

// Variadic
module["sum"] = new ChowDynamic((ChowValue[] args) =>
{
    int total = 0;
    foreach (ChowValue a in args) total += a.As<int>();
    return new ChowInt(total);
});

// Side-effect (returns None)
module["log"] = new ChowDynamic((ChowValue v) => Console.WriteLine(v));
```

## Inspecting `ChowValue` inside a delegate
```csharp
val.IsNone               // None check
val.Is<int>()            // tag check
val.As<int>()            // tag-typed cast (int / float / bool)
val is ChowStr  s        // string pattern match: s.Value
val is ChowList l        // list pattern match: l.Count, l[i]
val is ChowDynamic d     // arbitrary host object: d.Value
```

## Expression statement hook
To access the result of an evaluated expression statement, create a child class of `IExprStatementHook`. The example below is used by Chow.Repl:

```csharp
using Chow.Interpreter.Hooks;
using Chow.Interpreter.Values;

class PrintExprStatementHook : IExprStatementHook
{
    // This is called after each expression statement is evaluated
    // The 'value' parameter contains the result of the evaluated expression
    public void Invoke(ChowValue value) => Console.WriteLine(value);
}
```
Add an instance of the hook to a `ChowModule` object and execute your Chow source code:
```csharp
ChowModule module = new ChowModule();
module.AddHook(new PrintExprStatementHook());

// Prints:
// 19
// 1.5
module.Execute("9 + 10\n3 / 2");
```

## Example: REPL builtins
The Chow CLI registers `print`, `input`, `int`, `float`, `bool`, `str`, `len`, `type`, `abs`, `round`, `min`, `max` through this mechanism. Excerpt from `src/Chow.Cli/Program.cs`:
```csharp
module["print"] = new ChowDynamic((ChowValue val) =>
{
    Console.WriteLine(val);
    return ChowValue.None;
});

module["int"] = new ChowDynamic((ChowValue val) =>
{
    if (val.Is<int>())   return new ChowInt(val.As<int>());
    if (val.Is<float>()) return new ChowInt((int)val.As<float>());
    if (val is ChowStr s && int.TryParse(s.Value, out int parsed))
        return new ChowInt(parsed);
    throw new InvalidOperationException("int() argument unsupported");
});
```

Once registered, the names are callable from Chow source like any other function:
```python
n = int(input())
print(n * 2)
```

