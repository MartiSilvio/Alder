var obj = await Task.FromResult((object)42);
if (obj is int num)
{
    var doubled = await Task.FromResult(num * 2);
    if (doubled is int d && d > 50)
        return await Task.FromResult($"big:{d}");
    return await Task.FromResult($"small:{doubled}");
}
return "not int";
