var words = new[] { "hello", "world" };
var result = "";
foreach (var w in words)
{
    result += await Task.FromResult(w + " ");
}
return result.Trim();
