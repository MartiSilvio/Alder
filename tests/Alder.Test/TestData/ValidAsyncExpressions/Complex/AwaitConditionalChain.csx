var x = await Task.FromResult(10);
var y = x > 5 ? await Task.FromResult(x * 2) : await Task.FromResult(x);
return y;
