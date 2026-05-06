Func<int, Task<int>> f = async x => await Task.FromResult(x * 2);
return await f(5);
