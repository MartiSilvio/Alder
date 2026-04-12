// §15.15: Task.WhenAny with multiple completed tasks — check count is 2, winner kind is Task
var t1 = Task.FromResult("a");
var t2 = Task.FromResult("b");
var winner = await Task.WhenAny(t1, t2);
return winner.IsCompletedSuccessfully;
