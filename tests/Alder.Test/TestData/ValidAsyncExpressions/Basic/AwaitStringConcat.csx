var a = await Task.FromResult("hello");
var b = await Task.FromResult(" world");
return a + b;
