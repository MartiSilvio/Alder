var items = new[] { "a", "b", "c", "d", "e" };
var result = "";
var idx = 0;
foreach (var item in items)
{
    var prefix = await Task.FromResult(idx.ToString());
    result += $"{prefix}:{item} ";
    idx++;
}
return result.Trim();
