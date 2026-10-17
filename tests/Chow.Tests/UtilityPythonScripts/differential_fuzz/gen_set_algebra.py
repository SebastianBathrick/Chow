"""Random set-algebra programs: set methods and operators, dict_keys set operations, and & | ^ on ints.

Each program builds sets, a frozenset, a dict with its keys and values views, a list, a string and a
range, then applies random methods, operators and comparisons to them, including a set with itself.
It keeps changing the sets afterwards, so tables left full of dummies get reused. It avoids two
documented wording differences: ordering comparisons between unorderable types, and wrong method
arity.
"""

from __future__ import annotations

import random

SETS = ["a", "c"]                 # always plain sets
ANY_SETS = ["a", "b", "c"]        # b is a frozenset
ITERABLES = ["a", "b", "c", "k", "l", "t", "r", "d", "v"]
METHODS_VARARGS = ["union", "intersection", "difference"]
UPDATES_VARARGS = ["update", "intersection_update", "difference_update"]


def element(rng):
    roll = rng.random()
    if roll < 0.5:
        return str(rng.choice([rng.randrange(0, 70), rng.randrange(0, 9) * 8, rng.randrange(-3, 3),
                               2**61 + rng.randrange(3), -(2**61)]))
    if roll < 0.62:
        return rng.choice(["1.0", "2.5", "0.0", "-0.0", "8.0"])
    if roll < 0.7:
        return rng.choice(["True", "False"])
    if roll < 0.97:
        return repr(rng.choice(["a", "b", "g", "xyzzy", "q r", "é", "zz", ""]))
    return "frozenset([1, 2])"


def elements(rng, low=0, high=25):
    return "[" + ", ".join(element(rng) for _ in range(rng.randrange(low, high))) + "]"


def source(rng):
    kind = rng.random()
    if kind < 0.55:
        return elements(rng)
    if kind < 0.7:
        return f"range({rng.randrange(0, 30)}, {rng.randrange(30, 120)}, {rng.randrange(1, 9)})"
    if kind < 0.85:
        return "{" + ", ".join(f"{element(rng)}: 0" for _ in range(rng.randrange(0, 15))) + "}"
    return repr("".join(rng.choice("abcdefgh") for _ in range(rng.randrange(0, 10))))


def setup(rng):
    lines = [
        "log = []",
        f"a = set({source(rng)})",
        f"b = frozenset({source(rng)})",
        f"c = set({source(rng)})",
        "d = {" + ", ".join(f"{element(rng)}: 0" for _ in range(rng.randrange(0, 20))) + "}",
        "k = d.keys()",
        "v = d.values()",
        f"l = {elements(rng)}",
        f"t = {source(rng) if rng.random() < 0.3 else repr('abcxyz')}",
        f"r = range({rng.randrange(0, 20)}, {rng.randrange(20, 80)}, {rng.randrange(1, 6)})",
    ]
    # Rarely an unhashable element, which CPython and Chow must reject the same way.
    if rng.random() < 0.04:
        lines[7] = "l = [1, [2]]"
    return lines


def expression(rng):
    roll = rng.random()
    if roll < 0.3:
        op = rng.choice(["|", "&", "-", "^"])
        if rng.random() < 0.65:
            return f"{rng.choice(ANY_SETS)} {op} {rng.choice(ANY_SETS)}"
        # A keys view takes any iterable, on either side.
        other = rng.choice(ITERABLES)
        return f"k {op} {other}" if rng.random() < 0.5 else f"{other} {op} k"
    if roll < 0.42:
        op = rng.choice(["==", "!=", "<", "<=", ">", ">="])
        return f"{rng.choice(ANY_SETS + ['k'])} {op} {rng.choice(ANY_SETS + ['k'])}"
    if roll < 0.7:
        target = rng.choice(ANY_SETS)
        method = rng.choice(METHODS_VARARGS + ["symmetric_difference", "isdisjoint", "issubset", "issuperset"])
        if method in METHODS_VARARGS:
            args = ", ".join(rng.choice(ITERABLES) for _ in range(rng.choice([0, 1, 1, 2, 3])))
        else:
            args = rng.choice(ITERABLES)
        return f"{target}.{method}({args})"
    if roll < 0.75:
        return f"k.isdisjoint({rng.choice(ITERABLES)})"
    if roll < 0.76:
        # Rarely an operand pair no operator accepts, which ends the program with a TypeError.
        return f"{rng.choice(ANY_SETS)} {rng.choice(['|', '&', '-', '^'])} {rng.choice(['l', 't', 'd'])}"
    terms = [str(rng.choice([rng.randrange(-40, 40), rng.randrange(0, 2**20)])) if rng.random() < 0.8
             else rng.choice(["True", "False"]) for _ in range(rng.randrange(2, 5))]
    expr = terms[0]
    for term in terms[1:]:
        expr += f" {rng.choice(['&', '|', '^', '+', '-'])} {term}"
    return expr


def statement(rng):
    roll = rng.random()
    if roll < 0.4:
        return f"log.append({expression(rng)})"
    if roll < 0.55:
        target = rng.choice(SETS)
        method = rng.choice(UPDATES_VARARGS + ["symmetric_difference_update"])
        if method == "symmetric_difference_update":
            args = rng.choice(ITERABLES)
        else:
            args = ", ".join(rng.choice(ITERABLES) for _ in range(rng.choice([0, 1, 1, 2])))
        return f"{target}.{method}({args})\nlog.append(list({target}))"
    if roll < 0.68:
        # Results feed later operations, so tables full of dummies get reused.
        target = rng.choice(SETS)
        left = target if rng.random() < 0.7 else rng.choice(SETS)
        return f"{target} = {left} {rng.choice(['|', '&', '-', '^'])} {rng.choice(ANY_SETS)}"
    if roll < 0.8:
        target = rng.choice(SETS)
        return rng.choice([f"{target}.add({element(rng)})", f"{target}.discard({element(rng)})",
                           f"if {target}:\n    log.append({target}.pop())"])
    if roll < 0.88:
        return rng.choice([f"d[{element(rng)}] = 1", f"d.pop({element(rng)}, None)"])
    if roll < 0.94:
        return f"{rng.choice(SETS)} = {rng.choice(SETS)}.{rng.choice(METHODS_VARARGS)}({rng.choice(ITERABLES)})"
    return f"log.append(list({rng.choice(ANY_SETS)}))"


def program(rng):
    lines = setup(rng)
    for _ in range(rng.randrange(3, 25)):
        lines.append(statement(rng))
    lines.append("log.append([a, b, c, k])")
    lines.append("log = str(log)")
    return "\n".join(lines) + "\n"


def programs(count: int, seed: int) -> list[str]:
    rng = random.Random(seed)
    return [program(rng) for _ in range(count)]
