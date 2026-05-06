var funcs = new List<Func<int>>();
foreach (var x in new[] { 1, 2, 3 })
    funcs.Add(() => x);
return funcs[0]() + funcs[1]() + funcs[2]();
