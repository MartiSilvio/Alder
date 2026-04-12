// §15.15, §12.15: null-coalesce on awaited nullable string
string? s = null;
return s ?? await Task.FromResult("fallback");
