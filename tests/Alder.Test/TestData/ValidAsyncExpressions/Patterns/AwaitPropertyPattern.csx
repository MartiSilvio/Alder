var s = await Task.FromResult("hello world");
return s is { Length: > 5 };
