using Chow;

ChowValue value = ChowValue.CreateList();

ChowList list = value;

list.Append(0);
list.Append(2);
list[0] = -1;

Console.WriteLine(list);
