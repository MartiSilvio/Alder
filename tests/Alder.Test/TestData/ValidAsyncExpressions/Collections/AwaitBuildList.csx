var items = new List<int>();
for (var i = 0; i < 3; i++)
    items.Add(await Task.FromResult(i * 10));
return items.Sum();
