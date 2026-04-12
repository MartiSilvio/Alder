// §15.15: awaited value used as input to a LINQ Range computation
var n = await Task.FromResult(5);
return Enumerable.Range(1, n).Sum();
