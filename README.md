### Simple API Example
```csharp
using Chow;

ChowObject scope = ChowObject.CreateScope();

// Seed a variable from the host before running any Chow source.
scope["base_price"] = 100;

// Define some state. Variables persist in `scope`.
ChowEngine.Run("""

               tax_rate = 0.2
               total = base_price + (base_price * tax_rate)

               """, scope);

// Get the 'total' and 'tax_rate' variables from scope and store them
// in .NET variables
ChowObject total = scope["total"];
ChowObject taxRate = scope["tax_rate"];

Console.WriteLine($"tax_rate: {taxRate}");
Console.WriteLine($"total: {total}");
// Output:
// tax_rate: 0.2
// total: 120.0
```