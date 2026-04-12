// Named tuple elements accessed after OrderByDescending + First
var nums = new[] { 5, 3, 1, 4, 2 };
return nums
    .Select(n => (value: n, doubled: n * 2))
    .OrderByDescending(p => p.doubled)
    .First()
    .value;
