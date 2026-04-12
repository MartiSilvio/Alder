// §15.15: awaited nullable reference, conditional projects via ?.
string? s = await Task.FromResult<string?>("alder");
return s?.Length ?? -1;
