// §15.15: Task.WhenAny with a single completed task is deterministic
var t1 = Task.FromResult(100);
var winner = await Task.WhenAny(t1);
return await winner;
