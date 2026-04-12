// §15.15: Task.Delay(0) is already completed; followed by await of a value
await Task.Delay(0);
var v = await Task.FromResult("ok");
return v;
