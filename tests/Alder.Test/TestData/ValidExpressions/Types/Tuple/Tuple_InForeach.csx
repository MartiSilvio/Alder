// §13.9.5: foreach over list of tuples, accessing Item members
var pairs = new List<(int, int)> { (1, 2), (3, 4), (5, 6) };
int total = 0;
foreach (var p in pairs)
    total += p.Item1 + p.Item2;
return total;
