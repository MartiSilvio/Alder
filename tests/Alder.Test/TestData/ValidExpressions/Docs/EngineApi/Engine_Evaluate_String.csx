Enumerable.Range(1, 10)
    .Where(n => n % 2 == 0)
    .Select(n => n * n)
    .Sum()
