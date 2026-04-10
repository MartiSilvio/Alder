var funcs = new List<Func<int>>();
for (var i = 0; i < 3; i++) {
    var j = i;
    funcs.Add(() => j);
}
return funcs[0]() + funcs[1]() + funcs[2]();
