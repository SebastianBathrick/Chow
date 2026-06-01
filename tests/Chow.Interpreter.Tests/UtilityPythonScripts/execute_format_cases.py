import sys

EXPECTED_RESULT_PREFIX = "expected:"
FIELD_MARKER_PREFIX = "**"
REGION_MARKER_PREFIX = "*"
FIELD_DECL_INDENT = "    "
REGION_INDENT = "        "
CASE_INDENT = "            "


def is_expected_line(line: str) -> bool:
    return line.strip().lower().startswith(EXPECTED_RESULT_PREFIX)


def get_expected_result(line: str) -> str:
    return line.strip().split(":", 1)[1].strip()


def format_expected_value(expected: str) -> str:
    value = expected.strip()
    lower = value.lower()
    if lower == "true":
        return "TrueChow"
    if lower == "false":
        return "FalseChow"
    try:
        if "." in value:
            return f"new({float(value)})"
        return f"new({int(value)})"
    except ValueError:
        if (value.startswith('"') and value.endswith('"')) or (
            value.startswith("'") and value.endswith("'")
        ):
            if value.startswith("'"):
                return f'new("{value[1:-1]}")'
            return f"new({value})"
        return f'new("{value}")'


def print_region_start(name: str) -> None:
    print(f"{REGION_INDENT}#region {name}")
    print()


def print_region_end() -> None:
    print(f"{REGION_INDENT}#endregion")


def print_field_start(field_name: str) -> None:
    print(
        f"{FIELD_DECL_INDENT}static readonly IReadOnlyList<CaseExecute> {field_name} ="
    )
    print(f"{FIELD_DECL_INDENT}[")


def print_field_end() -> None:
    print(f"{FIELD_DECL_INDENT}];")


def print_case(case_lines: list[str], expected: str) -> None:
    body: list[str] = []
    for line in case_lines:
        if line.strip() == "":
            body.append("")
        else:
            body.append(f"{CASE_INDENT}{line}")

    content = "\n".join(body)
    expected_expr = format_expected_value(expected)

    print(f"""{REGION_INDENT}new(
{CASE_INDENT}\"\"\"
{content}
{CASE_INDENT}\"\"\",
{CASE_INDENT}{expected_expr}
{REGION_INDENT}),""")


def process_file(file_path: str) -> None:
    in_region = False
    in_field = False
    in_case = False
    case_lines: list[str] = []

    def close_region(*, before_new_region: bool = False) -> None:
        nonlocal in_region
        if in_region:
            print_region_end()
            if before_new_region:
                print()
            in_region = False

    def close_field(*, before_new_field: bool = False) -> None:
        nonlocal in_field
        if in_field:
            close_region()
            print_field_end()
            if before_new_field:
                print()
            in_field = False

    def flush_case(expected_line: str) -> None:
        nonlocal in_case, case_lines
        while case_lines and case_lines[-1].strip() == "":
            case_lines.pop()
        if not case_lines:
            return
        print_case(case_lines, get_expected_result(expected_line))
        print()
        case_lines = []
        in_case = False

    def open_field(field_name: str) -> None:
        nonlocal in_field
        print_field_start(field_name)
        in_field = True

    def open_region(region_name: str) -> None:
        nonlocal in_region
        print_region_start(region_name)
        in_region = True

    with open(file_path, encoding="utf-8") as file:
        for line in file:
            stripped = line.strip()

            if stripped.startswith(FIELD_MARKER_PREFIX):
                if in_case:
                    case_lines = []
                    in_case = False
                close_field(before_new_field=in_field)
                open_field(stripped[len(FIELD_MARKER_PREFIX) :])
                continue

            if stripped.startswith(REGION_MARKER_PREFIX):
                if in_case:
                    case_lines = []
                    in_case = False
                close_region(before_new_region=True)
                open_region(stripped[len(REGION_MARKER_PREFIX) :])
                continue

            if is_expected_line(line):
                flush_case(line)
                continue

            if stripped == "":
                if in_case:
                    case_lines.append(line.rstrip("\n\r"))
                continue

            in_case = True
            case_lines.append(line.rstrip("\n\r"))

    if in_case and case_lines:
        print(
            "Warning: trailing case without EXPECTED line was ignored.",
            file=sys.stderr,
        )

    close_field()


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python format_execute_case.py <UNFORMATTED EXECUTE CASE FILE>")
        sys.exit(1)

    process_file(sys.argv[1])
