var a = new List<int> { 1, 2, 3 };
var b = new DateTime(2024, 1, 1);
var c = new[] { 1, 2, 3 };
var d = new int[10];
var e = new int[,] { { 1, 2 }, { 3, 4 } };
return a.Count == 3 && b.Year == 2024 && c.Length == 3 && d.Length == 10 && e[1, 1] == 4;
