// §15.15: Task.WhenAny returns the first completed task which can then be awaited
var t1 = Task.FromResult(1);
var t2 = Task.FromResult(2);
var winner = await Task.WhenAny(t1, t2);
return await winner;
