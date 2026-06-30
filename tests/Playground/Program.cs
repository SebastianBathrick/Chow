using Chow;

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

try
{
    ChowEngine.Run("x = 1 / 0");
}
catch (RuntimeException ex)
{
    Console.WriteLine(ex.Message); // ZeroDivisionError: division by zero on line 1
}