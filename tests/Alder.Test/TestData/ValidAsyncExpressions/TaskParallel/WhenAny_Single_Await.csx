// §15.15: Task.WhenAny with a single task degenerates to that task
var t = Task.FromResult(99);
var winner = await Task.WhenAny(t);
return await winner;
