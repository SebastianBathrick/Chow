"""Random set-literal programs: how CPython 3.11 compiles set displays.

A display of three or more constants compiles to a frozenset constant, rebuilt from its own order,
and rebuilt once more when one of its strings had already been interned. Constants are folded
first (`2 * 3`), shared between literals with the same elements, and iterated directly by a for loop.

Modes:
- exact: numbers, folded expressions and strings CPython never interns.
- names: adds name-like strings, some also used as names in the program, and every single name
  character, the cases Chow models.
- startup: adds longer names that CPython interns at startup, which Chow can't know about (see
  CPYTHON_DIFFERENCES.md). Expect a few percent of mismatches.
"""

from __future__ import annotations

import json
import random
import string
import subprocess

from runners import cpython_env

FLOATS = ["0.5", "1.5", "-3.25", "100.25", "2.0"]
PLAIN_WORDS = ["a b", "x-y", "caf\u00e9", "\u4e2d", "!", "q r s", "1.5x", "?a", "", "\u00e9"]
FRESH_WORDS = ["qzxw", "zq1", "kx_9", "vvb", "Zork", "mmq", "ab_c", "Q2", "xyzzy", "plugh"]
SINGLE_CHARS = list(string.ascii_letters + string.digits + "_")
STARTUP_WORDS = ["name", "key", "value", "data", "path", "user", "error", "count", "type", "state"]
FOLDED = ["-3", "-(2 * 3)", "2 * 3", "1 + 1.5", "not True", "'ab' * 2", "10 // 3", "2 ** 10", "7 % 3", "0.5 * 4",
          "-True", "'x' + 'y z'", "3 - 10", "'qz' + 'xw'", "'zq' + '1'"]
# Statements that make a fresh word a name in the program without running anything that fails.
NAME_USES = ["{w} = 1", "def {w}():\n    pass", "def g_{n}({w}):\n    return {w}", "class P_{n}:\n    {w} = 1",
             "def h_{n}():\n    return f'{{{w}}}'", "def k_{n}():\n    global {w}\n    {w} = 2", "o_{n} = P()\no_{n}.{w} = 3"]


def check_words(python: str) -> None:
    """Confirms the word lists still fit this CPython: fresh words not interned at startup, startup words interned."""
    check = ("import sys, json\n"
             "print(json.dumps([w for w in json.loads(sys.argv[1]) "
             "if (lambda f: sys.intern(f) is not f)(''.join(list(w)))]))")
    out = subprocess.run([python, "-s", "-c", check, json.dumps(FRESH_WORDS + STARTUP_WORDS)],
                         capture_output=True, text=True, env=cpython_env(), check=True).stdout
    interned = set(json.loads(out))

    if interned & set(FRESH_WORDS):
        raise RuntimeError(f"CPython interns these at startup, so they aren't fresh: {interned & set(FRESH_WORDS)}")
    if set(STARTUP_WORDS) - interned:
        raise RuntimeError(f"CPython doesn't intern these at startup: {set(STARTUP_WORDS) - interned}")


def element(rng, mode):
    r = rng.random()
    if r < 0.45:
        return str(rng.choice([rng.randrange(-30, 300), rng.randrange(0, 64), 2**61 + rng.randrange(5)]))
    if r < 0.55:
        return rng.choice(FLOATS)
    if r < 0.6:
        return rng.choice(["True", "False"])
    if r < 0.7:
        return rng.choice(FOLDED)
    words = PLAIN_WORDS
    if mode in ("names", "startup"):
        words = words + FRESH_WORDS + SINGLE_CHARS
    if mode == "startup":
        words = words + STARTUP_WORDS
    return repr(rng.choice(words))


def literal(rng, mode, n, variable=False):
    items = [element(rng, mode) for _ in range(n)]
    if variable:
        items[rng.randrange(n)] = "v"
    return "{" + ", ".join(items) + "}"


def program(rng, mode):
    lines = ["log = []", f"v = {element(rng, mode)}", "class P:\n    pass"]
    if mode in ("names", "startup"):
        # Single-character names too: their literals should be unaffected.
        for n, w in enumerate(rng.sample(FRESH_WORDS + SINGLE_CHARS[:20], rng.randrange(0, 6))):
            use = rng.choice(NAME_USES if len(w) > 1 else NAME_USES[:1])
            lines.append(use.format(w=w, n=n))
    # Names may also come after the literals: CPython parses the whole module before compiling it.
    tail = []
    if mode in ("names", "startup") and rng.random() < 0.3:
        tail.append(f"{rng.choice(FRESH_WORDS)} = 0")
    for _ in range(rng.randrange(1, 5)):
        n = rng.choice([1, 2, 3, 4, 5, 6, 8, 12, 19, 20, 25, 33, 45, 60])
        kind = rng.random()
        if kind < 0.45:
            lines.append(f"log.append(list({literal(rng, mode, n)}))")
        elif kind < 0.6:
            lines.append(f"log.append(list({literal(rng, mode, n, variable=True)}))")
        elif kind < 0.8:
            lines.append(f"for x in {literal(rng, mode, n)}:")
            lines.append("    log.append(x)")
        elif kind < 0.9:
            # The same elements in two orders: CPython shares one constant between them.
            items = [element(rng, mode) for _ in range(n)]
            shuffled = items[:]
            rng.shuffle(shuffled)
            lines.append("log.append(list({" + ", ".join(items) + "}))")
            lines.append("log.append(list({" + ", ".join(shuffled) + "}))")
        else:
            body = literal(rng, mode, n)
            lines.append("def f():")
            lines.append(f"    return list({body})")
            lines.append("log.append(f())")
            lines.append(f"log.append(list({body}))")
    return "\n".join(lines + tail) + "\n"


def programs(count: int, seed: int, mode: str) -> list[str]:
    rng = random.Random(seed)
    return [program(rng, mode) for _ in range(count)]
