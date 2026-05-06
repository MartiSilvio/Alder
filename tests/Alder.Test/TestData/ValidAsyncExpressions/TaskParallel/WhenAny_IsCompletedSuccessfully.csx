// §15.15: awaited winner of WhenAny reports IsCompletedSuccessfully
var winner = await Task.WhenAny(Task.FromResult("a"), Task.FromResult("b"));
return winner.IsCompletedSuccessfully;
