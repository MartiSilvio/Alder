// §15.15: await Task<(int, int)> and deconstruct via Item1/Item2
var pair = await Task.FromResult((1, 2));
return pair.Item1 + pair.Item2;
