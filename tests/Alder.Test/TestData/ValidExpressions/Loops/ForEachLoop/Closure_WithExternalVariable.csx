var funcs = new List<Func<int>>(); var multiplier = 10; foreach (var i in new[] { 1, 2, 3 }) { var captured = i; funcs.Add(() => captured * multiplier); } return funcs[0]() + funcs[1]() + funcs[2]();
