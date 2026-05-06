// §15.15: await Task<int[]> then index the result
var arr = await Task.FromResult(new[] { 10, 20, 30 });
return arr[1];
