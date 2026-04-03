var parts = new[] { "a", "b", "c", "d" };
var result = "";
foreach (var p in parts)
{
    result += await Task.FromResult(p);
}
return result;
