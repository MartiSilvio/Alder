// §15.15: Task<T> declared locally then awaited
Task<int> t = Task.FromResult(5);
return await t;
