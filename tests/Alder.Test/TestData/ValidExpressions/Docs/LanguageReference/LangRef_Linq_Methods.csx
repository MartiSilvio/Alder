new[] { 5, 3, 8, 1, 9, 2, 7 }
    .Where(x => x > 3)
    .OrderBy(x => x)
    .Select(x => x * 10)
    .ToList()
