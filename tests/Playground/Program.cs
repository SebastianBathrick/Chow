using Chow;

var scope = new ChowScope();
scope["greet"] = ChowObject.Create((object name) => $"Greetings {name}.");

ChowEngine.Run(
    "print(greet())", scope); // 7


