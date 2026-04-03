var a = await Task.FromResult(1);
var b = await Task.FromResult(2);
return (a, b, a + b);
