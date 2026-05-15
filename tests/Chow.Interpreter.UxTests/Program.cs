using Chow.Interpreter;
using Chow.Interpreter.Values;


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
var result = module.ExecuteCall("add", new ChowInt(1), new ChowInt(3000));
Console.WriteLine(result);

