var a = await Task.FromResult<string>(null);
var b = await Task.FromResult<string>(null);
var c = await Task.FromResult("found");
return a ?? b ?? c;
