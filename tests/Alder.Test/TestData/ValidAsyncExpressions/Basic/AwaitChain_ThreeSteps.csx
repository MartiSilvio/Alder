// §15.15: three-step await chain, deterministic
var a = await Task.FromResult(1);
var b = await Task.FromResult(a * 2);
var c = await Task.FromResult(b * 2);
return c;
