// Named tuple pipeline with ToList and count
var nums = new[] { 10, 20, 30, 40 };
return nums
    .Select(n => (val: n, half: n / 2))
    .Where(p => p.half >= 10)
    .Count();
