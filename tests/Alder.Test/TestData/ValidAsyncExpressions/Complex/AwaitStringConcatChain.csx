// §15.15: three-way string concat from awaits
var a = await Task.FromResult("foo");
var b = await Task.FromResult("bar");
var c = await Task.FromResult("baz");
return a + b + c;
