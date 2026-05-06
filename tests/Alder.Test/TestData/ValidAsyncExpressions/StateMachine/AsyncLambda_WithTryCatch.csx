Func<int, Task<string>> safeDivide = async x => {
    try
    {
        var result = await Task.FromResult(100 / x);
        return result.ToString();
    }
    catch
    {
        return "error";
    }
};
var ok = await safeDivide(5);
var err = await safeDivide(0);
return $"{ok},{err}";
