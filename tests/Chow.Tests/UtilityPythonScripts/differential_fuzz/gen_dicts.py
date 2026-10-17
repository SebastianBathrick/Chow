"""Random dict programs: SourceDict's CPython 3.11 layout, iteration order, and mutation errors.

Each program builds a dict through random literal/dict()/merge/insert/pop/clear paths, then loops
over it, its keys or values view, or one of their reversed() iterators, while changing it at random
steps. The log records what each step yields, and the error shows when CPython raises
`RuntimeError: dictionary changed size during iteration` (or `keys changed`).
"""

from __future__ import annotations

import random

STR_KEYS = [f"k{i}" for i in range(24)]
NUM_KEYS = [0, 1, 2, 3, 5, 8, 13, 21, 2.5, True]


def key(rng, str_bias):
    return repr(rng.choice(STR_KEYS)) if rng.random() < str_bias else repr(rng.choice(NUM_KEYS))


def literal(rng, n, str_bias):
    return "{" + ", ".join(f"{key(rng, str_bias)}: {rng.randint(0, 99)}" for _ in range(n)) + "}"


def creation(rng, str_bias):
    lines = []
    kind = rng.randrange(5)
    if kind == 0:
        # Sizes around 16 cover CPython's chunked building of large literals.
        lines.append(f"d = {literal(rng, rng.choice([0, 1, 4, 5, 6, 10, 15, 16, 17, 20, 33, 40]), str_bias)}")
    elif kind == 1:
        lines.append(f"d = dict({literal(rng, rng.randint(0, 20), str_bias)})")
    elif kind == 2:
        lines.append(f"d = {literal(rng, rng.randint(0, 12), str_bias)} | {literal(rng, rng.randint(0, 12), str_bias)}")
    elif kind == 3:
        lines.append("d = {}")
        for _ in range(rng.randint(0, 25)):
            lines.append(f"d[{key(rng, str_bias)}] = {rng.randint(0, 99)}")
    else:
        lines.append(f"d = {literal(rng, rng.randint(0, 20), str_bias)}")
        lines.append("d = dict(d)")
    return lines


def mutation(rng, str_bias):
    roll = rng.random()
    if roll < 0.35:
        return f"d.pop({key(rng, str_bias)}, 0)"
    if roll < 0.75:
        return f"d[{key(rng, str_bias)}] = {rng.randint(0, 99)}"
    if roll < 0.85:
        return f"d.setdefault({key(rng, str_bias)}, 7)"
    if roll < 0.93:
        return f"d.update({literal(rng, rng.randint(0, 6), str_bias)})"
    if roll < 0.97:
        return "d.clear()"
    return "d = d | {}" if rng.random() < 0.5 else "d = dict(d)"


def program(rng):
    # All-str dicts use CPython's unicode table kind; any other key switches it to the general kind.
    str_bias = rng.choice([1.0, 1.0, 0.9, 0.5, 0.0])
    lines = ["log = []"] + creation(rng, str_bias)
    for _ in range(rng.randint(0, 8)):
        lines.append(mutation(rng, str_bias))
    source = rng.choice(["d", "d.keys()", "d.values()", "reversed(d)", "reversed(d.keys())", "reversed(d.values())"])
    lines.append("step = 0")
    lines.append(f"for x in {source}:")
    lines.append("    log.append(x)")
    for step in sorted(rng.sample(range(8), rng.randint(1, 4))):
        lines.append(f"    if step == {step}:")
        for _ in range(rng.randint(1, 3)):
            lines.append("        " + mutation(rng, str_bias))
    lines.append("    step = step + 1")
    return "\n".join(lines) + "\n"


def programs(count: int, seed: int) -> list[str]:
    rng = random.Random(seed)
    return [program(rng) for _ in range(count)]
