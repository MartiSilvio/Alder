new[] { "Alice", "Bob", "Charlie" }
    .Where(n => n.Length > 3)
    .Select(n => $"{n} ({n.Length})")
    .First()
