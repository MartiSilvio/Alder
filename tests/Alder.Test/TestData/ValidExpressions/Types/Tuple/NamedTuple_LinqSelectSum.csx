// Named tuple elements extracted before aggregate
var arr = new[] { 1, 2, 3, 4, 5 };
return arr
    .Select(x => (n: x, cube: x * x * x))
    .Where(p => p.cube > 10)
    .Count();
