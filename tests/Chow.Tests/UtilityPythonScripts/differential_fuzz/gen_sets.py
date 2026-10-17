"""Random set programs without set literals: hashing, SourceSet's CPython table, and its methods.

Sets are built with set() and frozenset() from lists, dicts, ranges and strings, then changed by
add/discard/remove/pop/clear/update/copy. The log records their order along the way, and a final
loop that changes the set checks `RuntimeError: Set changed size during iteration`.
"""

from __future__ import annotations

import random

# Floats with positional reprs only: Chow prints exponent forms as 1E+20 (a known bug).
FLOATS = ["0.5", "1.5", "-3.25", "100.25", "2.0", "1000000000000000.0"]
WORDS = ["a", "b", "ab", "xyz", "k_1", "caf\u00e9", "\u4e2d", "x y", "", "Q"]


def key(rng):
    r = rng.random()
    if r < 0.45:
        return str(rng.choice([rng.randrange(-50, 400), rng.randrange(0, 70), 2**61, -(2**61), 2**62 + 7]))
    if r < 0.6:
        return rng.choice(FLOATS)
    if r < 0.66:
        return rng.choice(["True", "False"])
    if r < 0.95:
        return repr(rng.choice(WORDS))
    return f"frozenset([{key(rng)}, {key(rng)}])"


def keys(rng, n):
    return "[" + ", ".join(key(rng) for _ in range(n)) + "]"


def creation(rng):
    kind = rng.randrange(6)
    if kind == 0:
        return "s = set()"
    if kind == 1:
        return f"s = set({keys(rng, rng.randrange(0, 40))})"
    if kind == 2:
        return "s = set({" + ", ".join(f"{key(rng)}: 0" for _ in range(rng.randrange(0, 25))) + "})"
    if kind == 3:
        return f"s = set(range({rng.randrange(0, 60)}))"
    if kind == 4:
        return f"s = set({repr(''.join(rng.choice('abcdefgh') for _ in range(rng.randrange(0, 12))))})"
    return f"s = set(frozenset({keys(rng, rng.randrange(0, 30))}))"


def mutation(rng):
    roll = rng.random()
    if roll < 0.38:
        return f"s.add({key(rng)})"
    if roll < 0.55:
        return f"s.discard({key(rng)})"
    if roll < 0.62:
        k = key(rng)
        return f"if {k} in s:\n        s.remove({k})"
    if roll < 0.7:
        return "if s:\n        log.append(s.pop())"
    if roll < 0.73:
        return "s.clear()"
    if roll < 0.81:
        return f"s.update({keys(rng, rng.randrange(0, 20))})"
    if roll < 0.86:
        return f"s.update(set({keys(rng, rng.randrange(0, 30))}))"
    if roll < 0.9:
        return "s.update({" + ", ".join(f"{key(rng)}: 1" for _ in range(rng.randrange(0, 15))) + "})"
    if roll < 0.95:
        return "s = s.copy()"
    return "s = set(frozenset(s))"


def program(rng):
    lines = ["log = []", creation(rng)]
    for _ in range(rng.randrange(0, 30)):
        # Mutations run at top level; indent continuation lines of if-blocks to match.
        lines.append(mutation(rng).replace("\n        ", "\n    "))
        if rng.random() < 0.3:
            lines.append("log.append(list(s))")
    lines.append("log.append(list(s))")
    lines.append("log.append(str(s))")
    lines.append("log.append(str(frozenset(s)))")
    lines.append(f"log.append([len(s), {key(rng)} in s, s == set(list(s)), s.issubset(list(s)), s.isdisjoint({keys(rng, 3)})])")
    if rng.random() < 0.3:
        lines.append("for x in s:")
        lines.append("    log.append(x)")
        lines.append(f"    s.{rng.choice(['add', 'discard'])}({key(rng)})")
    return "\n".join(lines) + "\n"


def programs(count: int, seed: int) -> list[str]:
    rng = random.Random(seed)
    return [program(rng) for _ in range(count)]
