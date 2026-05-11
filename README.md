# Chow

## Current Features
- Floating-point data type
- Integer data type
- Boolean data type
- String data type
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
- Callable interop functions


## Tests
REPL tests:
220/220 passing unit tests

## Small TODOs
- Get rid of "is dirty" flags in major classes (redundant)
- Remove top-level return statements (not needed)
- Add ChowSyntaxErrorException and use it for top-level return statements
- Add float literals starting with decimals
- Have the output on the REPL start at cursor positon 0 instead of 3

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