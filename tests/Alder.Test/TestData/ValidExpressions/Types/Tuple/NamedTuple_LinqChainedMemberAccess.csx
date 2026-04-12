// Named tuple element access with arithmetic in Where predicate
var data = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
return data
    .Select(x => (x: x, y: x * 2.0))
    .Where(p => p.x * p.x + p.y * p.y > 20.0)
    .Count();
