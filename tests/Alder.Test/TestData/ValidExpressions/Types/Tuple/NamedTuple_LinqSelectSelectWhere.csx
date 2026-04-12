// Named tuple elements in Select→Where with compound predicate
var nums = new[] { 1, 2, 3, 4, 5, 6 };
return nums
    .Select(n => (original: n, doubled: n * 2))
    .Where(p => p.original > 2 && p.doubled < 10)
    .Count();
