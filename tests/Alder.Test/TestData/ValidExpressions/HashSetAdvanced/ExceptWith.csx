var a = new HashSet<int> { 1, 2, 3 };
a.ExceptWith(new[] { 2 });
return a.OrderBy(x => x).ToArray();
