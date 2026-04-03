var x = 0;
try { x = await Task.FromResult(10); }
finally { await Task.Delay(1); }
return x;
