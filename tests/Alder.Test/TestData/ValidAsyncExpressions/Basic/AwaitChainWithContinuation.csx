// §15.15: await chain where second await depends on the first
var a = await Task.FromResult(10);
var b = await Task.FromResult(a + 5);
return b;
