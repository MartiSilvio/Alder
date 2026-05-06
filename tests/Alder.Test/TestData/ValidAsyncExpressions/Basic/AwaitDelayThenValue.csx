// §15.15: await Task.Delay(0) then evaluate a subsequent value
await Task.Delay(0);
return await Task.FromResult(99);
