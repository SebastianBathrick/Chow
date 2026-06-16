using Chow;

// Demonstrates running multiple Chow snippets against a single persistent scope,
// like a REPL: variables defined in one call stay available in the next.
//
// useBuiltIns is false here so the scope holds only our own variables (no built-in
// functions are imported); the snippets below use plain arithmetic, not built-ins.

var scope = new ChowScope();

// Seed a variable from the host before running any Chow source.
scope["base_price"] = 100L;

// First call: define some state. Variables persist in `scope`.
ChowEngine.Run(@"
tax_rate = 0.2
total = base_price + (base_price * tax_rate)
", scope, useBuiltIns: false);

// Second call: reuse state from the first call. The returned scope carries the
// value of the last expression statement in its ExpressionResult.
ChowObject result = scope["total"];

Console.WriteLine($"Variables in scope: {scope.Length}");           // base_price, tax_rate, total
Console.WriteLine($"tax_rate          : {scope["tax_rate"]}");      // read a variable back out
Console.WriteLine($"Last expression   : {result}"); // 120
