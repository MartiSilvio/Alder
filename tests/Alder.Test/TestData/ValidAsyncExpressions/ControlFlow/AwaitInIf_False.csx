var x = await Task.FromResult(false);
if (x) { return 42; }
return await Task.FromResult(0);
