using Chow.Interpreter;

// We will loop just for testing purposes in Visual Studio so we dont need to restart the program every time we want to test a new file or source code input. In production, this would likely be a one-time execution of a file or source code input and then the program would exit.
while (true)
{
    Console.WriteLine("Enter a file path (with extension or path separator) or inline source code:");
    Console.WriteLine("Escapes are supported: \\n, \\r, \\t, \\f, \\\\.");
    Console.Write("> ");
    string? input = Console.ReadLine();

    if (input == null)
    {
        break;
    }

    try
    {
        string sourceCode;
        if (LooksLikeFilePath(input))
        {
            sourceCode = ReadFileContents(input);
            sourceCode = DecodeEscapes(sourceCode);
        }
        else
        {
            sourceCode = DecodeEscapes(input);
        }
        ChowInstance instance = new ChowInstance();
        instance.Run(sourceCode);
        Console.WriteLine(instance.GetVariableDebugInfo());
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
}

static bool LooksLikeFilePath(string input)
{
    return input.Contains('/') || input.Contains('\\') || Path.HasExtension(input);
}

static string ReadFileContents(string filePath)
{
    if (string.IsNullOrWhiteSpace(filePath))
        throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

    if (!string.Equals(Path.GetExtension(filePath), ".chw", StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException($"File must have a .chw extension: {filePath}", nameof(filePath));

    string fullPath = Path.GetFullPath(filePath);

    if (!File.Exists(fullPath))
        throw new FileNotFoundException($"File not found at path: {fullPath}", fullPath);

    FileInfo fileInfo = new FileInfo(fullPath);
    if (fileInfo.Length == 0)
        throw new InvalidOperationException($"File is empty: {fullPath}");

    return File.ReadAllText(fullPath);
}

static string DecodeEscapes(string input)
{
    var decoded = new System.Text.StringBuilder(input.Length);
    bool isEscaped = false;

    foreach (char currentChar in input)
    {
        if (!isEscaped)
        {
            if (currentChar == '\\')
            {
                isEscaped = true;
                continue;
            }

            decoded.Append(currentChar);
            continue;
        }

        switch (currentChar)
        {
            case 'n':
                decoded.Append('\n');
                break;

            case 'r':
                decoded.Append('\r');
                break;

            case 't':
                decoded.Append('\t');
                break;

            case 'f':
                decoded.Append('\f');
                break;

            case '\\':
                decoded.Append('\\');
                break;

            default:
                decoded.Append('\\');
                decoded.Append(currentChar);
                break;
        }

        isEscaped = false;
    }

    if (isEscaped)
    {
        decoded.Append('\\');
    }

    return decoded.ToString();
}

// NOTE: This file is temporary test/development code.
// Chow.Cli will not have direct access to internal Chow library functionality.
