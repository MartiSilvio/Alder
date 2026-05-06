// §12.8.9.3: extension method — Zip combines two sequences
var a = new List<int> { 1, 2, 3 };
var b = new List<string> { "a", "b", "c" };
return a.Zip(b, (x, y) => $"{x}{y}").First();
