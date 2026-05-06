Func<Task<string>> greet = async () => await Task.FromResult("hello");
return await greet();
