var x = await Task.FromResult(true);
if (x) { return await Task.FromResult(42); }
return 0;
