var x = await Task.FromResult(5);
var doubled = await Task.FromResult(x * 2);
var y = x > 3 ? (doubled > 8 ? await Task.FromResult("big") : await Task.FromResult("medium")) : await Task.FromResult("small");
return y;
