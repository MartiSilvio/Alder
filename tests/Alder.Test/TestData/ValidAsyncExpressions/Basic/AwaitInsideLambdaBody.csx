// §15.15: async lambda with await in body, returning Task<int>
Func<Task<int>> f = async () => (await Task.FromResult(21)) * 2;
return await f();
