var x = await Task.FromResult<string>(null);
return x ?? "fallback";
