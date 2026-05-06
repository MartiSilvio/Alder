var keys = new[] { "a", "b", "c" };
var dict = new Dictionary<string, int>();
var multiplier = await Task.FromResult(10);
for (var i = 0; i < keys.Length; i++)
{
    var key = await Task.FromResult(keys[i]);
    dict[key] = await Task.FromResult((i + 1) * multiplier);
}
return dict["a"] + dict["b"] + dict["c"];
