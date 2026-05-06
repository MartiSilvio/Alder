var a = new[] { 1, 2, 3 };
var b = new[] { 10, 20, 30 };
return a.Zip(b, (x, y) => x + y).ToArray();
