# Chow

## Current Features
- Float, integer, and None types
- Arithmetic operations
- Nested expressions
- Top-level statements
- Variable declarations
- Variable assignments
- Top-level return statements

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