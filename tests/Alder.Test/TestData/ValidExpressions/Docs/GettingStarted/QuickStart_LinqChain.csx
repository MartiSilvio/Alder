new[] { "Alice", "Bob", "Charlie" }
    .Where(name => name.Length > 3)
    .Select(name => name.ToUpper())
    .ToList()
