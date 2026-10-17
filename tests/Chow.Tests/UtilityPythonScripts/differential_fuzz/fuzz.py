#!/usr/bin/env python3
"""Differential fuzzing: random programs run through both CPython 3.11 and Chow, results compared.

    python fuzz.py [suite ...] [--count N] [--seed S] [--dll PATH] [--python PATH] [--show N] [--save DIR]

With no suite named, runs every gated suite. Exits with 1 if a gated suite has a mismatch.
See README.md for what each suite covers and how to investigate a mismatch.
"""

from __future__ import annotations

import argparse
import random
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Callable

import gen_dict_layouts
import gen_dicts
import gen_set_algebra
import gen_set_literals
import gen_sets
from runners import SEP, build_chow, check_cpython, run_chow, run_cpython

DEFAULT_COUNT = 2000
MODELED_VERSION = "3.11"


@dataclass
class Suite:
    name: str
    description: str
    generate: Callable[[int, int], list[str]]
    # A gated suite must match CPython exactly; an ungated one covers a known gap, so only its rate is reported.
    gated: bool = True
    checks_words: bool = False


SUITES = [
    Suite("dicts", "dict layout, iteration order, views, reversed(), mutation errors",
          gen_dicts.programs),
    Suite("dict-layouts", "every small chunked-literal and table-kind case (fixed, ignores --count)",
          gen_dict_layouts.programs),
    Suite("sets", "set() and frozenset(): hashing, table layout, methods, mutation errors",
          gen_sets.programs),
    Suite("set-literals", "constant set displays: folding, sharing, rebuilding, for loops",
          lambda count, seed: gen_set_literals.programs(count, seed, "exact")),
    Suite("set-literals-names", "set displays with strings that are also names in the program",
          lambda count, seed: gen_set_literals.programs(count, seed, "names"), checks_words=True),
    Suite("set-algebra", "set operators and methods, dict_keys set operations, & | ^ on ints",
          gen_set_algebra.programs),
    Suite("set-literals-startup", "set displays with names CPython interns at startup (known gap)",
          lambda count, seed: gen_set_literals.programs(count, seed, "startup"), gated=False, checks_words=True),
]


def parse_args(argv: list[str] | None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("suites", nargs="*", metavar="suite", help="suites to run (default: every gated suite)")
    parser.add_argument("--count", type=int, default=DEFAULT_COUNT,
                        help=f"programs per random suite (default {DEFAULT_COUNT})")
    parser.add_argument("--seed", type=int, help="seed for the random suites (default: a new one, which is printed)")
    parser.add_argument("--dll", type=Path, help="a built Chow.dll to test (default: build src/Chow in Release)")
    parser.add_argument("--python", default=sys.executable,
                        help="the CPython to compare against (default: the one running this script)")
    parser.add_argument("--show", type=int, default=2, help="mismatching programs to print per suite (default 2)")
    parser.add_argument("--save", type=Path, help="folder to write each suite's mismatching programs to")
    parser.add_argument("--list", action="store_true", help="list the suites and exit")
    return parser.parse_args(argv)


def describe_mismatch(program: str, expected: str, actual: str) -> str:
    first_difference = next((i for i, (e, a) in enumerate(zip(expected, actual)) if e != a),
                            min(len(expected), len(actual)))
    indented = "\n".join("    " + line for line in program.rstrip("\n").split("\n"))
    return (f"{indented}\n  CPython: {expected}\n  Chow:    {actual}\n"
            f"  (first difference at character {first_difference})")


def main(argv: list[str] | None = None) -> int:
    # Logs can hold any character; don't let a narrow console encoding stop the report.
    sys.stdout.reconfigure(errors="backslashreplace")
    args = parse_args(argv)

    if args.list:
        for suite in SUITES:
            print(f"{suite.name:22} {suite.description}" + ("" if suite.gated else " [not gated]"))
        return 0

    by_name = {suite.name: suite for suite in SUITES}
    unknown = [name for name in args.suites if name not in by_name]

    if unknown:
        print(f"Unknown suite(s): {', '.join(unknown)}. Run with --list to see them.", file=sys.stderr)
        return 2

    suites = [by_name[name] for name in args.suites] if args.suites else [s for s in SUITES if s.gated]

    version = check_cpython(args.python)

    if version != MODELED_VERSION:
        print(f"Warning: comparing against CPython {version}. Chow models CPython {MODELED_VERSION}'s dict and "
              f"set internals, so other versions can legitimately disagree.", file=sys.stderr)

    if any(suite.checks_words for suite in suites):
        gen_set_literals.check_words(args.python)

    seed = args.seed if args.seed is not None else random.randrange(2**31)
    dll = args.dll or build_chow()
    print(f"CPython {version} ({args.python})\nChow    {dll}\nSeed    {seed}\n")

    failed = False

    for suite in suites:
        programs = suite.generate(args.count, seed)
        expected = run_cpython(programs, args.python)
        actual = run_chow(programs, dll)
        mismatches = [i for i, (e, a) in enumerate(zip(expected, actual)) if e != a]
        errors = sum(1 for result in expected if not result.endswith("|||"))

        line = f"{suite.name:22} {len(programs):5} programs, {len(mismatches)} mismatches ({errors} end in an error)"
        if not suite.gated:
            line += f", {1 - len(mismatches) / len(programs):.1%} match [not gated]"
        print(line)

        for index in mismatches[:args.show]:
            print(f"\n--- {suite.name} program {index}\n" + describe_mismatch(programs[index], expected[index], actual[index]))

        if mismatches and args.save:
            args.save.mkdir(parents=True, exist_ok=True)
            path = args.save / f"{suite.name}.txt"
            path.write_text(SEP.join(programs[i] for i in mismatches), encoding="utf-8", newline="\n")
            print(f"  Mismatching programs saved to {path}")

        if mismatches and args.show:
            print()

        failed |= bool(mismatches) and suite.gated

    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
