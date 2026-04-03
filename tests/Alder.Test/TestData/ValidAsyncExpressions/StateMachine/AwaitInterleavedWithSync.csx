var a = 1;
var b = await Task.FromResult(2);
var c = a + b;
var d = await Task.FromResult(c * 3);
var e = d - 1;
var extra = await Task.FromResult(10);
var f = await Task.FromResult(e + extra);
return f;
