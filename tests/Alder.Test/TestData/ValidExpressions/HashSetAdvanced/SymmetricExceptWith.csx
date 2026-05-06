var a = new HashSet<int> { 1, 2, 3 };
a.SymmetricExceptWith(new[] { 2, 3, 4 });
return a.OrderBy(x => x).ToArray();
