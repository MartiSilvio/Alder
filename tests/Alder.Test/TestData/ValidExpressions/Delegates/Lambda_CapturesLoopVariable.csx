// §12.19.6.3: foreach loop variable instantiated per-iteration (C# 5+)
var actions = new List<Func<int>>();
foreach (var i in new[] { 1, 2, 3 })
    actions.Add(() => i);
return actions[0]() + actions[1]() + actions[2]();
