#!/usr/bin/env python3
"""Shrinks a program where Chow and CPython disagree, so the cause is easier to see.

    python minimize.py <programs_file> [index] [--dll PATH] [--python PATH]

programs_file is a file `fuzz.py --save` wrote; index picks a program in it (default 0). Each round
tries deleting every line and every element of every bracketed list, keeps the shortest candidate
that still disagrees, and stops when nothing smaller does.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

from runners import SEP, build_chow, run_chow, run_cpython


def bracket_groups(line: str):
    """Yields the (start, end) span of the contents of every [...] and {...} in a line, nested ones too."""
    openings = []
    quote = None

    for i, char in enumerate(line):
        if quote:
            quote = None if char == quote else quote
        elif char in "'\"":
            quote = char
        elif char in "([{":
            openings.append((char, i))
        elif char in ")]}" and openings:
            opening, start = openings.pop()

            # Parentheses hold call arguments, which removing would mostly just break.
            if opening != "(":
                yield start + 1, i


def top_level_elements(text: str) -> list[str]:
    """Splits a bracket group's contents at the commas that aren't inside a nested group or string."""
    elements = []
    depth = 0
    quote = None
    start = 0

    for i, char in enumerate(text):
        if quote:
            quote = None if char == quote else quote
        elif char in "'\"":
            quote = char
        elif char in "([{":
            depth += 1
        elif char in ")]}":
            depth -= 1
        elif char == "," and depth == 0:
            elements.append(text[start:i].strip())
            start = i + 1

    elements.append(text[start:].strip())
    return elements


def candidates(program: str):
    lines = program.rstrip("\n").split("\n")

    for i in range(len(lines)):
        # A block header goes together with its body; either one alone wouldn't parse.
        end = i + 1

        if lines[i].rstrip().endswith(":"):
            indent = len(lines[i]) - len(lines[i].lstrip())

            while end < len(lines) and len(lines[end]) - len(lines[end].lstrip()) > indent:
                end += 1

        yield "\n".join(lines[:i] + lines[end:]) + "\n"

    for i, line in enumerate(lines):
        for start, end in bracket_groups(line):
            elements = top_level_elements(line[start:end])

            for j in range(len(elements)):
                shrunk = ", ".join(elements[:j] + elements[j + 1:])
                new_line = line[:start] + shrunk + line[end:]
                yield "\n".join(lines[:i] + [new_line] + lines[i + 1:]) + "\n"


def error_of(result: str) -> str:
    return result.split("|||", 1)[1]


def main(argv: list[str] | None = None) -> int:
    sys.stdout.reconfigure(errors="backslashreplace")
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("programs_file", type=Path)
    parser.add_argument("index", type=int, nargs="?", default=0)
    parser.add_argument("--dll", type=Path, help="a built Chow.dll to test (default: build src/Chow in Release)")
    parser.add_argument("--python", default=sys.executable, help="the CPython to compare against")
    args = parser.parse_args(argv)

    program = args.programs_file.read_text(encoding="utf-8").split(SEP)[args.index]
    dll = args.dll or build_chow()
    expected = run_cpython([program], args.python)[0]

    if run_chow([program], dll)[0] == expected:
        print("Chow and CPython agree on this program; nothing to shrink.")
        return 1

    # A candidate must fail the way the original does in CPython, so a deletion that merely breaks
    # the program (a NameError from a removed assignment) doesn't count as still disagreeing.
    original_error = error_of(expected)

    while True:
        # Strictly shorter, so the loop always ends (removing the only element of [] changes nothing).
        options = [option for option in dict.fromkeys(candidates(program)) if len(option) < len(program)]
        cpython_results = run_cpython(options, args.python)
        valid = [(option, result) for option, result in zip(options, cpython_results)
                 if error_of(result) == original_error]

        if not valid:
            break

        chow_results = run_chow([option for option, _ in valid], dll)
        still_disagreeing = [option for (option, result), chow in zip(valid, chow_results) if chow != result]

        if not still_disagreeing:
            break

        program = min(still_disagreeing, key=len)

    print(program)
    print("CPython:", run_cpython([program], args.python)[0])
    print("Chow:   ", run_chow([program], dll)[0])
    return 0


if __name__ == "__main__":
    sys.exit(main())
