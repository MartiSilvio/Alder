// §15.15: compute max from three awaited values
var a = await Task.FromResult(5);
var b = await Task.FromResult(12);
var c = await Task.FromResult(7);
return Math.Max(a, Math.Max(b, c));
