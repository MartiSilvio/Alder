// §15.15: ternary both branches are awaits
var flag = true;
return flag ? await Task.FromResult(1) : await Task.FromResult(2);
