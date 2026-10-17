"""Runs programs through CPython and through Chow, giving both results in one format.

A result is `<str(log)>|||<error>`: the program's global `log` as `str()` prints it, as it stood when
the program finished or failed, then the error that ended it as `Name: message`, or nothing.
"""

from __future__ import annotations

import os
import subprocess
import tempfile
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

SEP = "\n#####\n"
HERE = Path(__file__).resolve().parent
REPO_ROOT = HERE.parents[3]
CHOW_PROJECT = REPO_ROOT / "src" / "Chow"

# Each program gets a fresh interpreter. CPython's interned strings last for the life of a process
# and change how it builds constant sets, so programs sharing a process could change each other's
# results. The runner's own names are single letters: a one-character name never changes how a
# literal is interned, so the runner can't affect the program either.
CPYTHON_RUNNER = """\
import sys
s = {}
m = ''
try:
    exec(sys.stdin.read(), s)
except Exception as x:
    m = f'{type(x).__name__}: {x}' if str(x) else type(x).__name__
sys.stdout.write(f"{s['log'] if 'log' in s else ''}|||{m}")
"""


def cpython_env() -> dict[str, str]:
    # Chow orders sets as CPython does with string-hash randomization off. Never pass -I or -E:
    # both make CPython ignore PYTHONHASHSEED.
    return dict(os.environ, PYTHONHASHSEED="0", PYTHONIOENCODING="utf-8")


def check_cpython(python: str) -> str:
    """Confirms the interpreter runs with hash randomization off; returns its version, e.g. "3.11"."""
    probe = "import sys; print(sys.version_info[0], sys.version_info[1], sys.flags.hash_randomization)"
    done = subprocess.run([python, "-s", "-c", probe], capture_output=True, text=True, env=cpython_env(), check=True)
    major, minor, randomization = done.stdout.split()

    if randomization != "0":
        raise RuntimeError(f"{python} still randomizes string hashes with PYTHONHASHSEED=0")

    return f"{major}.{minor}"


def run_cpython(programs: list[str], python: str) -> list[str]:
    env = cpython_env()

    def run(source: str) -> str:
        # -s keeps user site-packages, and whatever they import at startup, out of the process.
        done = subprocess.run([python, "-s", "-c", CPYTHON_RUNNER], input=source.encode("utf-8"),
                              capture_output=True, env=env)
        if done.returncode != 0:
            return f"<CPython runner failed: {done.stderr.decode('utf-8', 'replace').strip()}>"
        return done.stdout.decode("utf-8")

    with ThreadPoolExecutor(max_workers=os.cpu_count() or 4) as pool:
        return list(pool.map(run, programs))


def build_chow() -> Path:
    """Builds Chow in Release into a folder outside the repo and returns the DLL's path.

    A separate artifacts folder also keeps the build clear of a running Chow.Repl, which holds the
    usual build output open.
    """
    artifacts = Path(tempfile.gettempdir()) / "chow-differential-fuzz"
    subprocess.run(["dotnet", "build", str(CHOW_PROJECT), "-c", "Release", "--nologo", "-v", "quiet",
                    "--artifacts-path", str(artifacts)], check=True)
    return artifacts / "bin" / "Chow" / "release" / "Chow.dll"


def run_chow(programs: list[str], dll: Path) -> list[str]:
    with tempfile.TemporaryDirectory() as folder:
        programs_path = Path(folder) / "programs.txt"
        results_path = Path(folder) / "results.txt"
        programs_path.write_text(SEP.join(programs), encoding="utf-8", newline="\n")

        # One PowerShell process loads Chow once and runs every program.
        subprocess.run(["pwsh", "-NoProfile", "-File", str(HERE / "run_chow.ps1"), "-DllPath", str(dll),
                        "-ProgramsPath", str(programs_path), "-ResultsPath", str(results_path)], check=True)
        results = results_path.read_text(encoding="utf-8").split(SEP)

    if len(results) != len(programs):
        raise RuntimeError(f"run_chow.ps1 returned {len(results)} results for {len(programs)} programs")

    return results
