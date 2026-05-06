var s = await Task.FromResult("hello");
var upper = await Task.FromResult(s.ToUpper());
var result = await Task.FromResult(upper + "!");
return result;
