// §15.15, §12.12.12: is-pattern on awaited value
var v = await Task.FromResult<object>(42);
return v is int n ? n + 1 : -1;
