var funcs = new List<Func<int>>();
for (var i = 0; i < 3; i++) {
    var j = i;
    funcs.Add(() => j);
}
funcs.Select(f => f()).ToList()