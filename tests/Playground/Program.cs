using Chow;
using Chow.Interpreter.Exceptions;

ChowObject result = ChowEngine.Run("1 + 2");
Console.WriteLine(result.ToString()); // 3

var scope = new ChowScope();
scope["name"] = "world";

ChowEngine.Run("greeting = f'Hello, {name}!'", scope);

ChowObject greeting = scope["greeting"];
Console.WriteLine(greeting.ToString()); // Hello, world!

scope = new ChowScope();
scope["greet"] = ChowObject.Create((object name) => $"Greetings {name}.");

ChowEngine.Run("print(greet(\"Linus\"))", scope); // Greetings Linus.

ChowProgram program = ChowEngine.Compile("total = price * quantity");

foreach (var (price, quantity) in new[] { (3L, 4L), (10L, 2L) })
{
    scope = new ChowScope();
    scope["price"] = price;
    scope["quantity"] = quantity;

    ChowEngine.Run(program, scope);
    Console.WriteLine(scope["total"].ToString()); // 12, then 20
}

try
{
    ChowEngine.Run("x = 1 / 0");
}
catch (CompileException ex)
{
    // The source code failed to compile, so none of it ran.
    Console.WriteLine(ex.Message);
}
catch (RuntimeException ex)
{
    Console.WriteLine(ex.Message); // ZeroDivisionError: division by zero on line 1
}

try
{
    ChowEngine.Compile("for i in range(3):\n    pass\nbreak");
}
catch (ChowException ex)
{
    Console.WriteLine(ex.Message); // SyntaxError: 'break' outside loop on line 3
}