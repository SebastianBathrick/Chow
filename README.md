# Chow

## Current Features

- Pythonic syntax
- REPL (`Chow.Cli`, assembly `chw`)
- Data types: `int`, `float`, `bool`, `str`, `None`, list, dict, function (source-defined)
- Opaque object passthrough — arbitrary host objects can be assigned to Chow globals and round-tripped back unchanged
- Internal extension point (`InteropClassObject`) for adding built-in "class-like" types with attribute/method
  dispatch (used internally; not part of the public host API yet)
- Arithmetic, comparison, and logical expressions (with chained comparisons)
- Membership (`in` / `not in`) on lists and dicts
- Top-level statements and expression statements
- Variable declaration and assignment; attribute assignment (`obj.attr = v`); subscript assignment (`a[k] = v`)
- `if` / `elif` / `else`
- `while` loops with `break` and `continue`
- `def` (function declaration) with closures
- List and dict literals, indexing, slicing, and built-in methods
- Host interop API: expose variables and callable functions to Chow source
- Expression-statement hook for REPL-style output

## Tests

456/456 passing tests

## Small TODOs

- Remove top-level return statements (not needed)
- Add ChowSyntaxErrorException and use it for top-level return statements
- Add float literals starting with decimals
- Have the output on the REPL start at cursor position 0 instead of 3
- Ensure negated primaries starting with "-" and "not" are flagged as such
- Add user-defined `class X:` syntax (separate from `InteropClassObject`, which is host-defined)

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
- **Dict:** `{1: 'a', 2: 'b'}`, `{}`

## Operators

- **Arithmetic:** `+`, `-`, `*`, `/`, `//`, `%`, `**`
- **Comparison:** `==`, `!=`, `<`, `>`, `<=`, `>=` (chained: `a < b < c` desugars to `a < b and b < c`)
- **Logical:** `and`, `or`, `not` (short-circuiting)
- **Membership:** `in`, `not in` (lists, dicts)
- **Unary:** `-x`, `not x`
- **Dict merge:** `|` (only on dicts; right-hand keys win)
- **Assignment:** `=` (variable, attribute, subscript)
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
# To access the result of an evaluated expression, register an `IExpressionStatementHook`.
9 + 10
```

## While Loops

```python
i = 0
while i < 5:
    print(i)
    i = i + 1

# `break` exits the nearest loop
while True:
    if i > 10:
        break
    i = i + 1

# `continue` jumps back to the loop condition
i = 0
while i < 5:
    i = i + 1
    if i == 3:
        continue
    print(i)   # prints 1, 2, 4, 5
```

`break` and `continue` outside a loop are caught at compile time. Variables first assigned inside a loop body are not
visible after the loop exits — block bodies have their own scope (see CLAUDE.md "Scoping" for details).

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

Slicing always returns a new list. A step of `0` raises an error. Slice assignment (`a[1:3] = [9, 9]`) is not yet
supported.

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

Methods are first-class values — they can be stored in variables and called later. The method stays bound to the
original list:

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

## Dicts

Dicts hold an ordered, mutable mapping of hashable keys to values. Insertion order is preserved. Keys may be `None`,
`bool`, `int`, `float`, or `str`; lists and dicts are not hashable and cannot be used as keys.

### Literals

```python
empty = {}
ages = {"alice": 30, "bob": 25}
mixed = {1: "i", "k": "s", None: "n", True: "t"}
nested = {1: {2: "inner"}}
```

### Subscript

```python
d = {1: "a", 2: "b"}

d[1]          # "a"
d[3] = "c"    # inserts; d is {1: "a", 2: "b", 3: "c"}
d[1] = "z"    # overwrites in place; insertion order preserved

d[99]         # KeyError: 99
d[[1]]        # TypeError: unhashable type: 'list'
```

### Methods

```python
d = {1: "a", 2: "b"}

d.get(1)              # "a"
d.get(99)             # None
d.get(99, "fallback") # "fallback"

d.pop(1)              # returns "a"; d is {2: "b"}
d.pop(99)             # KeyError
d.pop(99, "fallback") # "fallback"

d.setdefault(2, "z")  # returns "b" (existing); d unchanged
d.setdefault(3, "c")  # inserts and returns "c"

d.update({4: "d"})    # merges right into d; existing keys retain position, new keys append

d.clear()             # d is {}
```

Like list methods, dict methods are first-class values bound to the original dict:

```python
d = {}
f = d.setdefault
f(1, "a")
f(2, "b")
# d is {1: "a", 2: "b"}
```

Accessing an attribute that isn't a dict method raises an `AttributeError`:

```python
{}.fake
# AttributeError: 'dict' object has no attribute 'fake' on line 1
```

### Operators

```python
{1: "a", 2: "b"} | {2: "z", 3: "c"}   # {1: "a", 2: "z", 3: "c"}   merge (right wins)

{1: "a"} == {1: "a"}     # True   order-independent equality
{1: "a"} == {1: "b"}     # False
{1: {2: "x"}} == {1: {2: "x"}}  # True   recursive
```

### Membership (`in` / `not in`)

`in` and `not in` work on dicts (testing keys) and lists (testing elements):

```python
1 in {1: "a"}        # True
99 not in {1: "a"}   # True

2 in [1, 2, 3]       # True
9 not in [1, 2, 3]   # True

[1] in {1: "a"}      # TypeError: unhashable type: 'list'
1 in 5               # TypeError: argument of type 'Int' is not iterable
```

### Truthiness

Empty dicts are falsy, non-empty are truthy.

```python
if {}:
    "never"
else:
    "empty is falsy"
```

# Library API

Host C# code exposes variables and callable functions to Chow source through `ChowModule`. Variables flow in and out as
`ChowValue` instances; functions are wrapped in `ChowDynamic`.

## Exposing variables

```csharp
ChowModule module = new ChowModule();

// Host -> Chow
module["greeting"] = new ChowStr("Hello");
module["count"] = new ChowInt(42);

// Chow -> Host (after Execute)
module.Execute("result = count * 2");
int doubled = module.GetGlobal("result").As<int>();
```

The `ChowModule` indexer's getter returns the raw `object` boxed inside the `TaggedUnion` (`long`/`double`/`bool`/
`string`/etc.). Prefer `GetGlobal(name)` when you want a typed `ChowValue` to call `.As<T>()` / pattern-match against.
Reading an unset name throws `GlobalAccessException`.

## Exposing functions

Wrap a delegate in `ChowDynamic`. Supported signatures:

| Delegate                       | Behavior                  |
|--------------------------------|---------------------------|
| `Func<ChowValue>`              | Zero-arg, returns a value |
| `Func<ChowValue, ChowValue>`   | One-arg, returns a value  |
| `Func<ChowValue[], ChowValue>` | Variadic, returns a value |
| `Action`                       | Zero-arg, returns `None`  |
| `Action<ChowValue>`            | One-arg, returns `None`   |
| `Action<ChowValue[]>`          | Variadic, returns `None`  |

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
val.As<int>()            // tag-typed cast (int / float / bool / long / double)
val is ChowStr  s        // string pattern match: s.Value
val is ChowList l        // list pattern match: l.Count, l[i]
val is ChowDict d        // dict pattern match
val is ChowDynamic dyn   // arbitrary host object: dyn.Value
```

## Expression statement hook

To access the result of an evaluated expression statement, implement `IExpressionStatementHook`. The example below is
used by `Chow.Cli`:

```csharp
using Chow.Interpreter.Hooks;
using Chow.Interpreter.Values;

class PrintExprStatementHook : IExpressionStatementHook
{
    // Called after each expression statement is evaluated.
    // `value` is the result of the evaluated expression (cast from `object` to `ChowValue`).
    public void Invoke(object value = null) => Console.WriteLine((ChowValue)value);
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

The Chow CLI registers `print`, `input`, `int`, `float`, `bool`, `str`, `list`, `dict`, `len`, `type`, `abs`, `round`,
`min`, `max` through this mechanism. Excerpt from `src/Chow.Cli/Program.cs`:

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

