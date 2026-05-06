var a = new HashSet<int> { 1, 2 };
a.UnionWith(new[] { 2, 3 });
return a.OrderBy(x => x).ToArray();
