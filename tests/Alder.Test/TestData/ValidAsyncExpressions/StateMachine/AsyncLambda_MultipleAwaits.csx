Func<int, int, Task<int>> add = async (a, b) => {
    var x = await Task.FromResult(a);
    var y = await Task.FromResult(b);
    return x + y;
};
return await add(10, 20);
