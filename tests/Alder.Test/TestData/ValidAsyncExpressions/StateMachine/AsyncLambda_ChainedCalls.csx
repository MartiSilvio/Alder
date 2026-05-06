Func<int, Task<int>> step1 = async x => await Task.FromResult(x + 1);
Func<int, Task<int>> step2 = async x => await Task.FromResult(x * 3);
var a = await step1(5);
var b = await step2(a);
return b;
