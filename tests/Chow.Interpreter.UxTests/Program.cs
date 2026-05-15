using Chow.Interpreter;


ChowModule module = new ChowModule();

var code =
    """
    def add(x, y):
        print("Adding")
        return x + y

    """;
module.ImportBuiltIns();

module.Execute(code);
module.Execute("print(add(1, 2))");
var result = module.CallFunction("add", 1, 3000);
Console.WriteLine(result);

