// §15.15: async lambda with two awaits, result used
Func<Task<int>> f = async () =>
{
    var a = await Task.FromResult(3);
    var b = await Task.FromResult(4);
    return a + b;
};
return await f();
