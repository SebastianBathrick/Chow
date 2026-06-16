using Chow;

ChowValue value = ChowEngine.Execute(@"
employees = []
is_running = True

while is_running:
    employees.append(input(""Enter employee name: ""))
    is_running = input(""Continue? (y/n): "") != ""n""

employees
");

Console.WriteLine(value.ToString());
