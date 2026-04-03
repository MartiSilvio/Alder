var outer = await Task.FromResult(100);
var result = 0;
{
    var inner = await Task.FromResult(outer + 10);
    {
        var deepest = await Task.FromResult(inner + 1);
        result = deepest;
    }
}
return result;
