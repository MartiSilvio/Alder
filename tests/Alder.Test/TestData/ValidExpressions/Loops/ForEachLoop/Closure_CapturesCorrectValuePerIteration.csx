var funcs = new List<Func<int>>(); foreach (var i in new[] { 1, 2, 3 }) { var captured = i; funcs.Add(() => captured); } return funcs[0]() * 100 + funcs[1]() * 10 + funcs[2]();
