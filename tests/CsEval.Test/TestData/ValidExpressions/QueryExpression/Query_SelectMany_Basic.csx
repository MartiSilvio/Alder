var a = new[] { 1, 2 };
var b = new[] { 10, 20 };
return (from x in a from y in b select x + y).ToList();
