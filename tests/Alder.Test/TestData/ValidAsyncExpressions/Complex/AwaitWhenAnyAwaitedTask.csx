// §15.15: WhenAny with single completed task, then await the winner for its value
Task<string> t = Task.FromResult("done");
var winner = await Task.WhenAny(t);
var value = await winner;
return value.ToUpper();
