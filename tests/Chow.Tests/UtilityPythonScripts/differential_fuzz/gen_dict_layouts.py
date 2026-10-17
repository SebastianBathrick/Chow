"""Every small case of two dict layout details that random programs rarely reach.

- Chunked literals: CPython builds a literal of 16 or more pairs in chunks, so a literal that
  repeats keys can presize its table for more keys than it ends up holding.
- Table kinds: an all-str dict uses CPython's unicode table kind, and inserting a non-str key
  switches it to the general kind mid-iteration.

The suite is the same every run, so it ignores count and seed.
"""

from __future__ import annotations

import itertools


def chunk_programs():
    # Literals of 14-20 pairs over only `distinct` keys, so the pair count overstates the dict size.
    # Each step swaps the current key for a new one, keeping the size fixed until a resize compacts.
    for n, distinct in itertools.product(range(14, 21), range(1, 9)):
        pairs = ", ".join(f"'k{i % distinct}': {i}" for i in range(n))
        yield "\n".join(["log = []", f"d = {{{pairs}}}", "step = 0", "for x in d:", "    log.append(x)",
                         "    d.pop(x, 0)", "    d['n' + str(step)] = 0", "    step = step + 1"]) + "\n"


def table_kind_programs():
    for n, step, victim in itertools.product(range(1, 8), range(0, 3), range(0, 3)):
        pairs = ", ".join(f"'k{i}': {i}" for i in range(n))
        yield "\n".join(["log = []", f"d = {{{pairs}}}", "step = 0", "for x in d:", "    log.append(x)",
                         f"    if step == {step}:", f"        d.pop('k{victim}', 0)", "        d[5] = 0",
                         "    step = step + 1"]) + "\n"


def programs(count: int, seed: int) -> list[str]:
    return list(chunk_programs()) + list(table_kind_programs())
