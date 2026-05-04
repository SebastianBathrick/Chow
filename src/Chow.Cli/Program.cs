using Chow.Interpreter;
using Chow.Interpreter.Jit;
using Chow.Interpreter.Syntax;
using Chow.Interpreter.Syntax.Trees;
using Chow.Interpreter.Tokens;
using Chow.Interpreter.Evaluation;

Console.WriteLine("Enter an expression to parse. Use Ctrl+Z then Enter to quit.");
Console.WriteLine("Escapes are supported: \\n, \\r, \\t, \\f, \\\\.");

while (true)
{
    Console.Write("> ");
    string? input = Console.ReadLine();

    if (input == null)
    {
        break;
    }

    try
    {
        var scanner = new Scanner(DecodeEscapes(input));
        var parser = new Parser(scanner.ScanTokens());
        Node tree = parser.BuildSyntaxTree();

        Console.WriteLine(tree);

        var compiler = new Compiler(tree);
        Chunk chunk = compiler.CompileSyntaxTreeRoot();
        Console.WriteLine(chunk);

        var virtualMachine = new VirtualMachine(chunk);
        Console.WriteLine(virtualMachine.ExecuteChunk());
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
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
