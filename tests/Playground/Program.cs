using Chow;

var scope = new ChowScope();
var 
scope["add"] = ChowObject.Create();

ChowEngine.Run(
    "print(add(3, 4))", scope); // 7


