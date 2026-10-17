param([string]$DllPath, [string]$ProgramsPath, [string]$ResultsPath)

# Runs each program through ChowEngine in a fresh scope and writes one result per program, in the
# format runners.py describes: the program's log, '|||', then the error that ended it, if any.
Add-Type -Path $DllPath

$separator = "`n#####`n"
$programs = (Get-Content -Raw -Encoding utf8 $ProgramsPath) -split $separator

$results = foreach ($program in $programs) {
    $scope = [Chow.ChowScope]::new()
    $failure = ""

    try {
        [Chow.ChowEngine]::Run($program, $scope) | Out-Null
    }
    catch {
        # PowerShell wraps the exception a .NET method throws.
        $exception = $_.Exception.InnerException
        if (-not $exception) { $exception = $_.Exception }

        if ($exception -is [Chow.Interpreter.Exceptions.ChowException]) {
            # "ErrorName: description on line N" without the line, as CPython's runner prints it.
            $failure = $exception.Message -replace ' on line \d+$', ''
        }
        else {
            # Scripts must never raise a raw .NET exception, so this always counts as a mismatch.
            $failure = "INTERNAL $($exception.GetType().FullName): $($exception.Message)"
        }
    }

    "$($scope['log'])|||$failure"
}

[System.IO.File]::WriteAllText($ResultsPath, ($results -join $separator))
