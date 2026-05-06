// §15.15: compound condition from multiple awaited bools
var a = await Task.FromResult(true);
var b = await Task.FromResult(true);
var c = await Task.FromResult(false);
return a && b && !c;
