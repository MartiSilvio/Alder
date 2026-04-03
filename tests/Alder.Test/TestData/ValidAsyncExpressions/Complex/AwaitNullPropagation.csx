var s = await Task.FromResult<string>(null);
return s?.ToUpper() ?? "was null";
