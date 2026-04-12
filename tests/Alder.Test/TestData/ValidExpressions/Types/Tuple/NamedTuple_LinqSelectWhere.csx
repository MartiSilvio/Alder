// Named tuple element access across Select→Where LINQ chain
var items = new[] { 1, 2, 3, 4, 5 };
return items
    .Select(x => (val: x, squared: x * x))
    .Where(p => p.squared > 4)
    .Count();
