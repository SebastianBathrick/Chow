# Differences from CPython

Chow runs a subset of Python 3. This file lists the known places where the same code gives a
different result under Chow than under CPython, whether Chow doesn't support something yet or
supports it but behaves differently.

Every example was run through both Chow and CPython 3.11.0 (64-bit, Windows) on 2026-10-17.
Chow errors are shown the way the host receives them, as `ErrorName: message`. The `on line N`
suffix is left out.

## Status key

| Status | Meaning |
|---|---|
| **By design** | Deliberate. Matching CPython is impossible or not useful for an embedded interpreter. Not expected to change. |
| **Value model** | Follows from how Chow stores values: 64-bit integers and UTF-16 strings. Changing it means redesigning `SourceValue`. |
| **Planned** | On the current roadmap. |
| **Not supported** | Missing for now. Could be added. |
| **Bug** | Chow gives the wrong result or message and should be fixed. |

## Syntax

`del`, `import` and `yield` aren't keywords in Chow, so it parses them as names. That's why they
fail with `NameError` rather than `SyntaxError`.

| Feature | Example | Chow | Status |
|---|---|---|---|
| Conditional expressions | `1 if c else 2` | `SyntaxError` | Not supported |
| `lambda` | `lambda x: x` | `SyntaxError` | Not supported |
| Comprehensions | `[x for x in y]` | `SyntaxError` | Not supported |
| Tuples and unpacking | `(1, 2)`, `a, b = 1, 2` | `SyntaxError` | Not supported |
| Augmented assignment | `x += 1` | `SyntaxError` | Not supported |
| Chained assignment | `a = b = 1` | `SyntaxError` | Not supported |
| Default parameters, keyword arguments, `*args`, `**kwargs` | `def f(x=1)`, `f(x=1)` | `SyntaxError` | Not supported |
| Exceptions: `try`/`except`/`finally`, `raise`, `assert` | `try:` | `SyntaxError` (the keywords are reserved but not parsed) | Not supported |
| `with` | `with open('x') as f:` | `SyntaxError` (reserved keyword) | Not supported |
| `del` | `del d['a']` | `NameError: name 'del' is not defined` | Not supported |
| `import` | `import math` | `NameError: name 'import' is not defined` | Not supported |
| Generators | `yield 1` | `NameError: name 'yield' is not defined` | Not supported |
| Decorators | `@d` | `SyntaxError: Unexpected character '@'` | Not supported |
| `while`/`else` | `while c: ... else: ...` | `SyntaxError` (`for`/`else` works) | Not supported |
| Assignment expressions | `(n := 3)` | `SyntaxError` | Not supported |
| Semicolons | `a = 1; b = 2` | `SyntaxError: Unexpected character ';'` | Not supported |
| Backslash line continuation | `1 + \` | `SyntaxError: Unexpected character '\'` (lines continue fine inside brackets) | Not supported |
| Unary `+` | `+1` | `SyntaxError` | Not supported |
| `&`, `^` and `\|` on integers | `5 & 3`, `1 \| 2` | `SyntaxError` for `&`/`^`; `\|` raises `TypeError` (Chow only defines it for dict merge) | Planned |
| `~`, `<<`, `>>` | `~5`, `1 << 2` | `SyntaxError` | Not supported |
| Set literals | `{1, 2}` | `SyntaxError` | Planned |
| Slice assignment | `a[0:1] = [9]` | `SyntaxError: slice assignment is not supported` | Not supported |
| `for` targets other than a plain name | `for p.x in items:` | `SyntaxError` | Not supported |
| Class inheritance | `class B(A):` | `SyntaxError` | Not supported |

## Literals

| Feature | Example | Chow | Status |
|---|---|---|---|
| Float exponents | `1e3` | `NameError: name 'e3' is not defined` (read as `1` followed by a name) | Not supported |
| Hex, octal and binary integers | `0x1F`, `0b101` | `NameError: name 'x1F' is not defined` | Not supported |
| Underscores in numbers | `1_000` | `NameError: name '_000' is not defined` | Not supported |
| Integers beyond 64 bits | `99999999999999999999` | `SyntaxError: int literal out of range` | Value model |
| Triple-quoted strings | `'''a'''` | `SyntaxError: Unterminated string literal` | Not supported |
| Raw and bytes strings | `r'a\n'`, `b'a'` | `NameError: name 'r' is not defined` | Not supported |
| `\x`, `\u`, `\U`, `\N{...}` and octal escapes | `'\x41'` | `SyntaxError: Unknown escape sequence '\x'`. Only `\n \t \r \\ \' \" \0 \a \b \f \v` are supported. | Not supported |
| Unrecognized escapes | `'\d'` | `SyntaxError`. CPython keeps the backslash and warns. | Not supported |
| Non-ASCII identifiers | `café = 1` | `SyntaxError: Unexpected character 'é'` | Not supported |
| f-string format specs and conversions | `f'{x:.2f}'`, `f'{x!r}'` | `SyntaxError` | Not supported |

## Numbers

| Behavior | Example | CPython | Chow | Status |
|---|---|---|---|---|
| Integers are 64-bit and wrap silently on overflow | `9223372036854775807 + 1` | `9223372036854775808` | `-9223372036854775808` | Value model |
| | `2 ** 64` | `18446744073709551616` | `0` | Value model |
| `int()` of large or underscored strings | `int('99999999999999999999')`, `int('1_000')` | the number | `ValueError: invalid literal for int()` | Value model / Not supported |
| `==` between int and float above 2**53 converts the int to float | `9007199254740993 == 9007199254740992.0` | `False` | `True` (dict keys compare these exactly) | Bug |
| Exponent letter in float output | `str(10.0 ** 17)` | `1e+17` | `1E+17` | Bug |
| Floats from 1e16 up to 1e17 | `str(10.0 ** 16)` | `1e+16` | `10000000000000000.0` | Bug |
| Infinity and NaN output | `str(10.0 ** 308 * 10.0)`, `str(float('nan'))` | `inf`, `nan` | `Infinity.0`, `NaN.0` | Bug |
| Parsing infinity | `float('inf')` | `inf` | `ValueError: could not convert string to float: 'inf'` | Bug |
| Float overflow in `**` | `10.0 ** 400` | `OverflowError` | `Infinity.0` | Bug |
| Complex numbers | `(-8) ** (1 / 3)` | a `complex` | `NaN.0` | Not supported |
| `ZeroDivisionError` messages | `1 // 0`, `1 % 0`, `1.0 // 0.0` | `integer division or modulo by zero`, `integer modulo by zero`, `float floor division by zero` | `division by zero` for all | Bug |
| `round()` with digits | `round(2.675, 2)` | `2.67` (2.675 is stored as 2.67499…) | `2.68` | Bug |
| Attributes on numbers | `(1).real` | `1` | `AttributeError` | Not supported |

## Strings

| Behavior | Example | CPython | Chow | Status |
|---|---|---|---|---|
| String methods | `'abc'.upper()` | `'ABC'` | `AttributeError` (strings have no methods) | Not supported |
| Indexing and slicing | `'abc'[0]`, `'abc'[0:2]` | `'a'`, `'ab'` | `TypeError: 'str' object is not subscriptable` | Not supported |
| Substring `in` | `'a' in 'abc'` | `True` | `TypeError: argument of type 'str' is not iterable` | Not supported |
| `%` formatting and `.format()` | `'%d' % 5` | `'5'` | `TypeError` / `AttributeError` | Not supported |
| Length and iteration count UTF-16 units, not code points | `len('😀')` | `1` | `2` | Value model |
| Ordering compares UTF-16 units | `'😀' > '！'` | `True` | `False` (only characters above U+FFFF compared with U+E000–U+FFFF differ) | Value model |

## Collections

| Behavior | Example | CPython | Chow | Status |
|---|---|---|---|---|
| Ordering comparisons on lists | `[1] < [2]` | `True` | `TypeError` | Not supported |
| List methods beyond append, clear, insert, pop, remove, reverse | `a.sort()`, `a.index(x)`, `a.count(x)`, `a.extend(b)`, `a.copy()` | works | `AttributeError` | Not supported |
| Dict methods beyond clear, get, keys, pop, setdefault, update, values | `d.items()`, `d.copy()`, `d.popitem()`, `dict.fromkeys(k)` | works | `AttributeError` (`items()` needs tuples) | Not supported |
| Set operations on `dict_keys` | `d.keys() & {'a'}` | a `set` | `SyntaxError` | Planned |
| `dict_keys.mapping`, `dict_values.mapping` | `d.keys().mapping` | a `mappingproxy` | `AttributeError` | Not supported |
| `set` and `frozenset` | `set([1])` | a set | `NameError` | Planned |
| Range indexing, slicing and `in` | `range(5)[2]`, `2 in range(5)` | `2`, `True` | `TypeError` | Not supported |
| Range equality | `range(3) == range(3)` | `True` | `False` (compares identity) | Bug |
| Hashing ranges, functions, classes and instances | `{range(3): 1}`, `{f: 1}`, `{P(): 1}` | works | `TypeError: unhashable type` | Not supported |
| Containers check identity before `==` | `x = float('nan')` then `[x] == [x]`, `x in [x]` | `True`, `True` | `False`, `False` | Bug |
| Self-referencing containers | `a = [1]` then `a.append(a)` then `str(a)` or `a == a` | `[1, [...]]`, `True` | **Crashes the host process** with an uncatchable `StackOverflowException`. A dict holding its own `values()` view does the same. | Bug |

Changing a dict while iterating it matches CPython exactly: when it raises
`RuntimeError: dictionary changed size during iteration` (or `keys changed`), and which keys are
yielded first. Chow models CPython 3.11's dict internals as built on 64-bit platforms. Other
CPython versions, or 32-bit builds, may size tables differently and so give different results in
those edge cases. (By design.)

## Built-in functions

Chow has `print`, `input`, `len`, `abs`, `round`, `min`, `max`, `range`, `iter`, `next`,
`reversed` and the type constructors `int`, `float`, `str`, `bool`, `list` and `dict`. Any other
built-in raises `NameError`.

| Behavior | Example | CPython | Chow | Status |
|---|---|---|---|---|
| Other built-ins | `type`, `isinstance`, `sum`, `sorted`, `enumerate`, `zip`, `map`, `filter`, `any`, `all`, `chr`, `ord`, `repr`, `hash`, `id`, `open` | available | `NameError` | Not supported |
| Keyword arguments to built-ins | `print(1, 2, sep='-')`, `min(a, key=abs)` | works | `SyntaxError` (no keyword arguments anywhere) | Not supported |
| `iter(callable, sentinel)` | `iter(f, None)` | a `callable_iterator` | `TypeError: iter() takes 1 to 1 arguments` | Not supported |
| `clear()` | `clear()` | `NameError` | clears the console | By design (Chow-only) |
| Built-in arity errors | `len()` | `len() takes exactly one argument (0 given)` | `len() takes 1 to 1 arguments` | Bug (wording) |

## Functions, scope and classes

| Behavior | Example | CPython | Chow | Status |
|---|---|---|---|---|
| Reading a local before it's assigned | `x = 1`, then `def f(): y = x; x = 2` | `UnboundLocalError` | reads the global `x` | Bug |
| Missing-argument message | `f(1)` for `def f(a, b)` | `f() missing 1 required positional argument: 'b'` | `f() takes 2 positional arguments but 1 were given` | Bug (wording) |
| Recursion limit | runaway recursion | `RecursionError: maximum recursion depth exceeded` | `RecursionError: maximum recursion depth of 1000 exceeded`. Default limit 1000, as in CPython; configurable through `ChowRunSettings`, and host callbacks count toward it. | By design |
| Reprs leave out memory addresses and the module | `str(f)`, `str(P())`, `str(P)`, `str(iter([]))` | `<function f at 0x…>`, `<__main__.P object at 0x…>`, `<class '__main__.P'>`, `<list_iterator object at 0x…>` | `<function f>`, `<P object>`, `<class 'P'>`, `<list_iterator object>` | By design |
| Dunder methods aren't called | `__str__`, `__eq__`, `__len__`, … | used | ignored: `str(P())` gives `<P object>` | Not supported |
| Inheritance, `super()`, decorators such as `@classmethod` | `class B(A):`, `super()`, `@classmethod` | works | `SyntaxError` (`super()` raises `NameError`) | Not supported |
| Class attribute error message | `P.x` | `type object 'P' has no attribute 'x'` | `'P' object has no attribute 'x'` | Bug (wording) |
| `is` on numbers and bools compares values | `a = int('1000')` then `a is 1000` | `False` (int caching is a CPython detail) | `True` | By design |
| Built-in methods are cached per object | `a.append is a.append` | `False` | `True` | By design |

## Errors

| Behavior | CPython | Chow | Status |
|---|---|---|---|
| Handling errors in a script | `try`/`except` | none: every error ends the script and reaches the host as a `ChowException` | Not supported |
| `next()` on an exhausted iterator | raises a catchable `StopIteration` | raises `StopIteration`, which a script can't catch. Use `next(it, default)`. | Not supported |
| Error format | traceback | `ErrorName: message on line N` | By design |
| Syntax error messages | describe the code, e.g. `expected ':'`; indentation problems raise `IndentationError` | name Chow's token types, e.g. `expected 'SymbolColon'`; indentation problems raise `SyntaxError: expected 'Indent'` | Bug (wording) |
| Ordering `TypeError` message | `'<' not supported between instances of 'int' and 'str'` | `unsupported operand type(s) for <: 'int' and 'str'` | Bug (wording) |
| Warnings | e.g. `SyntaxWarning` for `x is 1` | none | Not supported |
