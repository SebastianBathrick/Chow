#!/usr/bin/env python3
"""Compare ChowEngine CaseExecute tests against Python 3 execution results."""

from __future__ import annotations

import ast
import os
import re
import shutil
import subprocess
import sys
import textwrap
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

# Subprocess prints this prefix so we can parse the result line from stdout
RESULT_SENTINEL = "__CHOW_PY_RESULT__:"
DEFAULT_CS_PATH = (
    Path(__file__).resolve().parent.parent / "LanguageTests" / "LanguageFeatureTests.cs"
)

# Parsed from LanguageFeatureTests.cs at runtime — no hard-coded const values
RE_CONST_STRING = re.compile(
    r"^\s*const\s+string\s+(\w+)\s*=\s*(.+?);\s*(?://.*)?$"
)
RE_CONST_INT = re.compile(
    r"^\s*const\s+int\s+(\w+)\s*=\s*(-?\d+)\s*;\s*(?://.*)?$"
)
RE_CONST_DOUBLE = re.compile(
    r"^\s*const\s+double\s+(\w+)\s*=\s*(-?\d+(?:\.\d+)?)\s*;\s*(?://.*)?$"
)
RE_STATIC_CHOW = re.compile(
    r"^\s*static\s+readonly\s+ChowValue\s+(\w+)\s*=\s*new\((true|false)\)\s*;\s*$"
)
RE_CASE_LIST = re.compile(
    r"static\s+readonly\s+IReadOnlyList<CaseExecute>\s+(\w+)\s*="
)

# Long-lived Python worker: fresh namespace per case, last-expression semantics like ChowEngine.Execute
PYTHON_DRIVER = f'''
import ast
import sys
import traceback

SENTINEL = {RESULT_SENTINEL!r}

def eval_last_expr(source, ns):
    tree = ast.parse(source)
    if tree.body and isinstance(tree.body[-1], ast.Expr):
        stmts, last = tree.body[:-1], tree.body[-1]
        if stmts:
            exec(compile(ast.Module(stmts, []), "<chow>", "exec"), ns, ns)
        return eval(compile(ast.Expression(last.value), "<chow>", "eval"), ns, ns)
    exec(compile(tree, "<chow>", "exec"), ns, ns)
    return None

def run_case(source):
    ns = {{}}
    try:
        result = eval_last_expr(source, ns)
        print(SENTINEL + repr(result))
    except Exception:
        print(SENTINEL + "<Python Error: " + traceback.format_exc().splitlines()[-1] + ">")

while True:
    line = sys.stdin.readline()
    if not line:
        break
    line = line.rstrip("\\n")
    if line == "__CHOW_PY_QUIT__":
        break
    if line.startswith("__CHOW_PY_RUN__"):
        source = line[len("__CHOW_PY_RUN__"):].replace("\\\\n", "\\n")
        run_case(source)
        sys.stdout.flush()
'''


@dataclass
class SymbolTables:
    symbols: dict[str, Any] = field(default_factory=dict)


@dataclass
class CaseExecuteEntry:
    list_name: str
    index: int
    region: str | None
    raw_source_expr: str  # C# first arg to CaseExecute, e.g. '"1" + PLUS + "2"' or '"""..."""'
    raw_expected_expr: str  # C# second arg, e.g. 'new(3)' or 'TrueChow'
    resolved_source: str | None = None
    resolved_expected: Any = None
    resolve_errors: list[str] = field(default_factory=list)


def find_python_executable() -> str:
    # PYTHON env var overrides PATH lookup (full path or command name)
    if env_python := os.environ.get("PYTHON"):
        path = shutil.which(env_python) if os.path.sep not in env_python and "/" not in env_python else env_python
        if path and Path(path).exists():
            return path
        raise RuntimeError(f"PYTHON is set but not found: {env_python!r}")

    names = ("python", "python3") if sys.platform == "win32" else ("python3", "python")
    for name in names:
        if path := shutil.which(name):
            return path
    raise RuntimeError(f"Could not find {' or '.join(names)} on PATH")


def normalize_csharp_raw_string(content: str) -> str:
    # C# """ literals in LanguageFeatureTests.cs include file formatting indent; strip like textwrap.dedent
    if content.startswith("\r\n"):
        content = content[2:]
    elif content.startswith("\n"):
        content = content[1:]
    elif content.startswith("\r"):
        content = content[1:]

    content = textwrap.dedent(content)
    return content.rstrip(" \t\r\n")


def decode_csharp_string_literal(literal: str) -> str:
    literal = literal.strip()
    if literal.startswith('"""') and literal.endswith('"""'):
        return normalize_csharp_raw_string(literal[3:-3])

    if not (literal.startswith('"') and literal.endswith('"')):
        raise ValueError(f"Not a C# string literal: {literal!r}")

    raw = literal[1:-1]
    result: list[str] = []
    i = 0
    while i < len(raw):
        ch = raw[i]
        if ch != "\\":
            result.append(ch)
            i += 1
            continue
        i += 1
        if i >= len(raw):
            raise ValueError(f"Trailing backslash in string literal: {literal!r}")
        esc = raw[i]
        if esc == "n":
            result.append("\n")
        elif esc == "r":
            result.append("\r")
        elif esc == "t":
            result.append("\t")
        elif esc == "\\":
            result.append("\\")
        elif esc == '"':
            result.append('"')
        elif esc == "0":
            result.append("\0")
        elif esc == "f":
            result.append("\f")
        elif esc == "a":
            result.append("\a")
        elif esc == "b":
            result.append("\b")
        elif esc == "v":
            result.append("\v")
        else:
            result.append(esc)
        i += 1
    return "".join(result)


def parse_const_and_field_tables(cs_text: str) -> SymbolTables:
    tables = SymbolTables()
    for line in cs_text.splitlines():
        stripped = line.strip()
        if stripped.startswith("//"):
            continue  # skip commented-out consts (e.g. TRUTHY_RANGE)

        match = RE_CONST_STRING.match(line)
        if match:
            name, value_expr = match.group(1), match.group(2).strip()
            value_expr = value_expr.split("//", 1)[0].strip()
            tables.symbols[name] = decode_csharp_string_literal(value_expr)
            continue

        match = RE_CONST_INT.match(line)
        if match:
            tables.symbols[match.group(1)] = int(match.group(2))
            continue

        match = RE_CONST_DOUBLE.match(line)
        if match:
            tables.symbols[match.group(1)] = float(match.group(2))
            continue

        match = RE_STATIC_CHOW.match(line)
        if match:
            tables.symbols[match.group(1)] = match.group(2) == "true"  # TrueChow / FalseChow

    return tables


def _skip_whitespace(text: str, pos: int) -> int:
    while pos < len(text) and text[pos] in " \t\r\n":
        pos += 1
    return pos


def _read_string_literal(text: str, pos: int) -> tuple[str, int]:
    pos = _skip_whitespace(text, pos)
    if text.startswith('"""', pos):
        end = text.find('"""', pos + 3)
        if end == -1:
            raise ValueError("Unterminated raw string literal")
        return text[pos : end + 3], end + 3

    if text[pos] != '"':
        raise ValueError(f"Expected string literal at {pos}")

    i = pos + 1
    while i < len(text):
        if text[i] == "\\":
            i += 2
            continue
        if text[i] == '"':
            return text[pos : i + 1], i + 1
        i += 1
    raise ValueError("Unterminated string literal")


def _read_identifier_token(text: str, pos: int) -> tuple[str, int]:
    pos = _skip_whitespace(text, pos)
    start = pos
    while pos < len(text) and (text[pos].isalnum() or text[pos] in "._!"):
        pos += 1
    if start == pos:
        raise ValueError(f"Expected identifier at {pos}")
    return text[start:pos], pos


def read_expression_until(text: str, pos: int, stop_at_comma_depth: int) -> tuple[str, int]:
    # Scan first/second CaseExecute ctor args; respects strings and nested parens
    pos = _skip_whitespace(text, pos)
    start = pos
    depth = 0
    in_string = False
    string_quote = ""
    raw_triple = False

    while pos < len(text):
        if raw_triple:
            if text.startswith('"""', pos):
                pos += 3
                raw_triple = False
                in_string = False
            else:
                pos += 1
            continue

        if in_string:
            if text[pos] == "\\":
                pos += 2
                continue
            if text[pos] == string_quote:
                in_string = False
                string_quote = ""
            pos += 1
            continue

        if text.startswith('"""', pos):
            raw_triple = True
            in_string = True
            pos += 3
            continue

        ch = text[pos]
        if ch == '"':
            in_string = True
            string_quote = '"'
            pos += 1
            continue

        if ch == "(":
            depth += 1
            pos += 1
            continue
        if ch == ")":
            if depth == 0:
                break  # end of second arg (e.g. after new(3))
            depth -= 1
            pos += 1
            continue
        if ch == "," and depth == stop_at_comma_depth:
            break  # comma between source and expected args
        pos += 1

    return text[start:pos].strip(), pos


def split_plus_expression(expr: str) -> list[str]:
    # Split C# string concatenation at top-level '+' (e.g. "1" + PLUS + "2")
    parts: list[str] = []
    pos = 0
    depth = 0
    in_string = False
    string_quote = ""
    raw_triple = False
    segment_start = 0

    while pos <= len(expr):
        if pos == len(expr):
            part = expr[segment_start:pos].strip()
            if part:
                parts.append(part)
            break

        if raw_triple:
            if expr.startswith('"""', pos):
                pos += 3
                raw_triple = False
                in_string = False
            else:
                pos += 1
            continue

        if in_string:
            if expr[pos] == "\\":
                pos += 2
                continue
            if expr[pos] == string_quote:
                in_string = False
                string_quote = ""
            pos += 1
            continue

        if expr.startswith('"""', pos):
            raw_triple = True
            in_string = True
            pos += 3
            continue

        ch = expr[pos]
        if ch == '"':
            in_string = True
            string_quote = '"'
            pos += 1
            continue
        if ch == "(":
            depth += 1
            pos += 1
            continue
        if ch == ")":
            depth -= 1
            pos += 1
            continue
        if (
            ch == "+"
            and depth == 0
            and not in_string
            and pos + 1 < len(expr)
            and expr[pos + 1] == " "
        ):
            part = expr[segment_start:pos].strip()
            if part:
                parts.append(part)
            pos += 1
            segment_start = pos
            continue
        if ch == "+" and depth == 0 and not in_string:
            part = expr[segment_start:pos].strip()
            if part:
                parts.append(part)
            pos += 1
            segment_start = pos
            continue
        pos += 1

    return parts


def resolve_source_segment(segment: str, tables: SymbolTables) -> str:
    segment = segment.strip()
    if segment == "string.Empty":
        return ""
    if segment == "null!":
        return ""  # Chow treats null source as empty
    if segment.startswith('"') or segment.startswith('"""'):
        return decode_csharp_string_literal(segment)
    if segment not in tables.symbols:
        raise KeyError(f"Unknown identifier: {segment}")
    value = tables.symbols[segment]
    if isinstance(value, bool):
        return "True" if value else "False"
    return str(value)  # e.g. NOT + TRUTHY_INT64 -> "not 1"


def resolve_source_expr(expr: str, tables: SymbolTables) -> tuple[str | None, list[str]]:
    errors: list[str] = []
    expr = expr.strip()
    try:
        if "+" in expr:
            parts = split_plus_expression(expr)
            return "".join(resolve_source_segment(part, tables) for part in parts), errors
        return resolve_source_segment(expr, tables), errors
    except (KeyError, ValueError) as exc:
        errors.append(str(exc))
        return None, errors


def parse_new_constructor_arg(arg: str) -> Any:
    arg = arg.strip()
    if arg == "ChowValue.None":
        return None

    if arg.startswith("new"):
        paren_start = arg.find("(")
        paren_end = arg.rfind(")")
        if paren_start == -1 or paren_end == -1:
            raise ValueError(f"Invalid new expression: {arg!r}")
        inner = arg[paren_start + 1 : paren_end].strip()
        if inner == "ChowValue.None":
            return None
        if inner in ("true", "false"):
            return inner == "true"
        if inner.startswith('"') or inner.startswith('"""'):
            return decode_csharp_string_literal(inner)
        if "." in inner:
            return float(inner)
        return int(inner)

    raise ValueError(f"Unsupported expected expression: {arg!r}")


def resolve_expected_expr(expr: str, tables: SymbolTables) -> tuple[Any | None, list[str]]:
    errors: list[str] = []
    expr = expr.strip().rstrip(",")
    try:
        if expr in tables.symbols:
            return tables.symbols[expr], errors  # TrueChow / FalseChow
        if expr == "ChowValue.None":
            return None, errors
        return parse_new_constructor_arg(expr), errors
    except (KeyError, ValueError) as exc:
        errors.append(str(exc))
        return None, errors


def parse_case_execute_entries(cs_text: str) -> list[CaseExecuteEntry]:
    entries: list[CaseExecuteEntry] = []
    list_name = ""
    in_case_list = False
    bracket_depth = 0  # track [ ... ] of each IReadOnlyList<CaseExecute>
    current_region: str | None = None
    case_index = 0
    i = 0
    text = cs_text

    while i < len(text):
        list_match = RE_CASE_LIST.search(text, i)
        if list_match and list_match.start() == i:
            list_name = list_match.group(1)
            in_case_list = True
            bracket_depth = 0
            case_index = 0
            i = list_match.end()
            continue

        if not in_case_list:
            i += 1
            continue

        if text.startswith("#region", i):
            line_end = text.find("\n", i)
            if line_end == -1:
                line_end = len(text)
            region_line = text[i:line_end]
            name_start = region_line.find("region") + len("region")
            current_region = region_line[name_start:].strip()
            i = line_end + 1
            continue

        if text.startswith("#endregion", i):
            current_region = None
            line_end = text.find("\n", i)
            i = len(text) if line_end == -1 else line_end + 1
            continue

        if text[i] == "[":
            bracket_depth += 1
            i += 1
            continue
        if text[i] == "]":
            bracket_depth -= 1
            if bracket_depth <= 0:
                in_case_list = False
                list_name = ""
                current_region = None
            i += 1
            continue

        if bracket_depth <= 0:
            i += 1
            continue

        if text.startswith("new(", i):
            pos = i + len("new(")
            raw_source, pos = read_expression_until(text, pos, stop_at_comma_depth=0)
            if pos < len(text) and text[pos] == ",":
                pos += 1
            raw_expected, pos = read_expression_until(text, pos, stop_at_comma_depth=0)
            if pos < len(text) and text[pos] == ")":
                pos += 1

            case_index += 1
            entries.append(
                CaseExecuteEntry(
                    list_name=list_name,
                    index=case_index,
                    region=current_region,
                    raw_source_expr=raw_source,
                    raw_expected_expr=raw_expected,
                )
            )
            i = pos
            continue

        i += 1

    return entries


class PythonReplRunner:
    def __init__(self, python_executable: str) -> None:
        self._proc = subprocess.Popen(
            [python_executable, "-u", "-c", PYTHON_DRIVER],
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            encoding="utf-8",
        )

    def run_source(self, source: str) -> str:
        if self._proc.stdin is None or self._proc.stdout is None:
            raise RuntimeError("Python subprocess is not running")

        # Encode newlines so entire source fits on one stdin line
        payload = source.replace("\n", "\\n")
        self._proc.stdin.write(f"__CHOW_PY_RUN__{payload}\n")
        self._proc.stdin.flush()

        lines: list[str] = []
        while True:
            line = self._proc.stdout.readline()
            if not line:
                break
            line = line.rstrip("\n")
            if line.startswith(RESULT_SENTINEL):
                return line[len(RESULT_SENTINEL) :]
            lines.append(line)

        trailing = "\n".join(lines).strip()
        if trailing:
            return f"<Python Error: {trailing}>"
        return "<Python Error: no result from Python subprocess>"

    def close(self) -> None:
        if self._proc.stdin is not None:
            self._proc.stdin.write("__CHOW_PY_QUIT__\n")
            self._proc.stdin.flush()
        self._proc.wait(timeout=5)


def format_display_value(value: Any) -> str:
    if isinstance(value, str):
        return repr(value)
    return repr(value)


def values_equivalent(expected: Any, python_repr: str) -> bool:
    try:
        python_value = ast.literal_eval(python_repr)
    except (SyntaxError, ValueError):
        return False
    if expected == python_value:
        return True
    if isinstance(expected, float) and isinstance(python_value, float):
        return abs(expected - python_value) < 1e-9
    return False


# ANSI colors — only applied when stdout is a TTY (see colorize)
GREEN = "\033[32m"
RED = "\033[31m"
YELLOW = "\033[33m"
CYAN = "\033[36m"
BLUE = "\033[34m"
MAGENTA = "\033[35m"
WHITE = "\033[37m"
BOLD = "\033[1m"
DIM = "\033[2m"
RESET = "\033[0m"


def enable_terminal_colors() -> None:
    # Enable VT100 escape sequences on Windows consoles
    if sys.platform == "win32":
        try:
            import ctypes

            kernel32 = ctypes.windll.kernel32
            handle = kernel32.GetStdHandle(-11)
            mode = ctypes.c_uint32()
            if kernel32.GetConsoleMode(handle, ctypes.byref(mode)):
                kernel32.SetConsoleMode(handle, mode.value | 0x0004)
        except (AttributeError, OSError):
            pass


def colorize(text: str, color: str) -> str:
    if not sys.stdout.isatty():
        return text
    return f"{color}{text}{RESET}"


def styled_header(text: str) -> str:
    return colorize(text, BOLD + CYAN)


def styled_label(text: str) -> str:
    return colorize(text, BOLD + BLUE)


def styled_source(text: str) -> str:
    return colorize(text, WHITE)


def styled_warning(text: str) -> str:
    return colorize(text, YELLOW)


def styled_expected_value(text: str) -> str:
    return colorize(text, MAGENTA)


def styled_python_value(text: str) -> str:
    return colorize(text, CYAN)


def format_labeled_line(label: str, value: str, value_styler) -> str:
    if not sys.stdout.isatty():
        return f"{label}{value}"
    return styled_label(label) + value_styler(value)


def is_case_matching(case: CaseExecuteEntry, python_result_repr: str | None) -> bool:
    if python_result_repr is None or python_result_repr.startswith("<Python Error:"):
        return False
    if case.resolve_errors:
        return False
    return values_equivalent(case.resolved_expected, python_result_repr)


def format_match_status(case: CaseExecuteEntry, python_result_repr: str | None) -> str:
    if is_case_matching(case, python_result_repr):
        return colorize("MATCHING", GREEN)
    return colorize("NOT MATCHING", RED)


def format_case_report(
    case: CaseExecuteEntry, python_result_repr: str | None
) -> str:
    lines: list[str] = []
    header = f"=== {case.list_name} #{case.index} ==="
    if case.region:
        header += f" [{case.region}]"
    lines.append(styled_header(header))
    lines.append(styled_label("Source:"))
    if case.resolved_source is not None:
        lines.append(styled_source(case.resolved_source))
    else:
        lines.append(styled_warning(f"<unresolved: {case.raw_source_expr}>"))

    if case.resolved_expected is not None or case.raw_expected_expr.strip() == "ChowValue.None":
        expected_display = format_display_value(case.resolved_expected)
    elif case.resolve_errors:
        expected_display = f"<unresolved: {case.raw_expected_expr}>"
    else:
        expected_display = format_display_value(case.resolved_expected)

    if case.resolve_errors and case.resolved_expected is None and case.raw_expected_expr.strip() != "ChowValue.None":
        lines.append(format_labeled_line("Expected (Chow): ", expected_display, styled_warning))
    else:
        lines.append(format_labeled_line("Expected (Chow): ", expected_display, styled_expected_value))

    if python_result_repr is None:
        if case.resolve_errors:
            lines.append(
                format_labeled_line(
                    "Python result:    ",
                    f"<skipped: {'; '.join(case.resolve_errors)}>",
                    styled_warning,
                )
            )
        else:
            lines.append(format_labeled_line("Python result:    ", "<skipped>", styled_warning))
    elif python_result_repr.startswith("<Python Error:"):
        lines.append(format_labeled_line("Python result:    ", python_result_repr, styled_warning))
    else:
        lines.append(format_labeled_line("Python result:    ", python_result_repr, styled_python_value))

    if case.resolve_errors:
        lines.append(format_labeled_line("Resolve notes: ", "; ".join(case.resolve_errors), styled_warning))

    lines.append(format_match_status(case, python_result_repr))

    return "\n".join(lines)


def load_and_prepare_cases(cs_path: Path) -> tuple[list[CaseExecuteEntry], SymbolTables]:
    cs_text = cs_path.read_text(encoding="utf-8")
    tables = parse_const_and_field_tables(cs_text)
    entries = parse_case_execute_entries(cs_text)

    for case in entries:
        source, source_errors = resolve_source_expr(case.raw_source_expr, tables)
        expected, expected_errors = resolve_expected_expr(case.raw_expected_expr, tables)
        case.resolved_source = source
        case.resolved_expected = expected
        case.resolve_errors = source_errors + expected_errors

    return entries, tables


def main(argv: list[str] | None = None) -> int:
    args = argv if argv is not None else sys.argv[1:]
    cs_path = Path(args[0]) if args else DEFAULT_CS_PATH
    if not cs_path.is_file():
        print(f"File not found: {cs_path}", file=sys.stderr)
        return 1

    enable_terminal_colors()

    entries, _tables = load_and_prepare_cases(cs_path)
    python_executable = find_python_executable()
    runner = PythonReplRunner(python_executable)

    try:
        for case in entries:
            python_result: str | None
            if case.resolved_source is None:
                python_result = None  # skip Python when C# source could not be resolved
            else:
                python_result = runner.run_source(case.resolved_source)
            print(format_case_report(case, python_result))
            if sys.stdout.isatty():
                print(colorize("─" * 72, DIM))
            else:
                print()
    finally:
        runner.close()

    return 0  # informational only — mismatches do not fail the script


if __name__ == "__main__":
    raise SystemExit(main())
