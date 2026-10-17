# Differential fuzzing against CPython

Chow reproduces CPython 3.11's dict and set internals, so iteration order, which element of two
equal ones survives, and when a mutation during iteration raises, all match CPython exactly. Unit
tests check chosen cases. These scripts check random ones: they generate thousands of programs, run
each through CPython and through Chow, and compare the results.

## Running

```
python tests/Chow.Tests/UtilityPythonScripts/differential_fuzz/fuzz.py
```

This builds `src/Chow` in Release into a temporary folder, runs every gated suite with 2,000
programs each (about a minute), prints a line per suite, and exits with 1 if any program's results
differ. It isn't part of CI.

Requirements:
- **CPython 3.11.** Other versions can legitimately disagree, and the script warns if it finds one.
  It uses the Python that runs it; pass `--python PATH` to use another.
- **PowerShell 7** (`pwsh`) on PATH, which loads Chow and runs the programs.
- **The .NET SDK**, for the build. Pass `--dll PATH` to test an existing `Chow.dll` instead.

Useful options:

| Option | Effect |
|---|---|
| `fuzz.py sets set-algebra` | Run only the named suites |
| `--list` | List the suites |
| `--count N` | Programs per random suite (default 2000) |
| `--seed S` | Repeat a run. Each run prints its seed; by default a new seed is picked |
| `--show N` | Mismatching programs to print per suite (default 2) |
| `--save DIR` | Write each suite's mismatching programs to `DIR/<suite>.txt` |

## Suites

| Suite | What it covers |
|---|---|
| `dicts` | Dict layout and iteration order through literals, `dict()`, `\|`, inserts, pops and `clear()`; views and `reversed()`; `RuntimeError` when the dict changes during iteration |
| `dict-layouts` | Every small case of two layout details: chunked building of literals with 16+ pairs, and the switch from the str-only table kind. Fixed, so `--count` and `--seed` don't apply |
| `sets` | `set()` and `frozenset()` without literals: CPython's hashes (SipHash-1-3 for strings), the table, every non-algebra method, `pop()` order, `RuntimeError` on a size change |
| `set-literals` | Set displays: constant folding, the frozenset constant and its rebuilds, sharing between equal literals, `for` loops over a literal |
| `set-literals-names` | Set displays with strings that CPython may already have interned: names in the program, and single characters |
| `set-algebra` | `\| & - ^` and `< <= > >=` on sets, their methods, `dict_keys` set operations from either side, and `& \| ^` on ints and bools |
| `set-literals-startup` | **Not gated.** Set displays with longer names CPython interns at startup, which Chow can't know about. Reports its match rate (about 97%); see `CPYTHON_DIFFERENCES.md` |

## Results

Each program records what it observes in a global list, `log`. A result is `str(log)`, then `|||`,
then the error that ended the program as `Name: message`, or nothing. So a mismatch can be in the
values, in their order, or in when and how a program fails. A raw .NET exception from Chow shows as
`INTERNAL …` and never matches.

## Investigating a mismatch

`--save` writes the mismatching programs to a file. `minimize.py` then shrinks one: it keeps
deleting lines, blocks and list elements while Chow and CPython still disagree.

```
python tests/Chow.Tests/UtilityPythonScripts/differential_fuzz/fuzz.py set-algebra --seed 123 --save fuzz-out
python tests/Chow.Tests/UtilityPythonScripts/differential_fuzz/minimize.py fuzz-out/set-algebra.txt 0
```

The index picks a program in the file (default 0).

## Pitfalls these scripts handle, and why

- **Hash randomization.** CPython randomizes string hashes unless `PYTHONHASHSEED` is set. Chow
  orders sets as CPython does with `PYTHONHASHSEED=0`, so the runner sets it. Never add `-I` or `-E`:
  both make CPython ignore `PYTHONHASHSEED`. Both quietly change every string set's order.
- **One process per program.** CPython interns strings for the life of a process. That decides
  whether it rebuilds a constant frozenset, which can change a set literal's order. Programs that
  share a process, through `exec` for example, can change each other's results. The runner's own
  variables are single letters, which never affect interning.
- **Whole programs.** Each program compiles as one module, as Chow compiles it. Compiling parts
  separately would stop CPython sharing constants between them.
- **Documented differences are avoided, not matched.** The generators don't produce floats that
  print in exponent form, ordering comparisons between unorderable types, or wrong method arity.
  Chow differs from CPython on each of these, as `CPYTHON_DIFFERENCES.md` lists.

## Adding a suite

Write a `gen_<name>.py` module with a `programs(count, seed)` function that returns program source
strings. Each program should start with `log = []` and append what it observes. Then add a `Suite`
for it in `fuzz.py`. Keep generated programs within what Chow supports and away from its documented
differences. Otherwise every program mismatches for a reason that's already known.
