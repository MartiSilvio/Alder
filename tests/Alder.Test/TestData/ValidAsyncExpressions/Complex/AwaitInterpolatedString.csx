// §15.15: awaited values used in an interpolated string
var name = await Task.FromResult("alder");
var version = await Task.FromResult(1);
return $"{name} v{version}";
