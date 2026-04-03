var flag = await Task.FromResult(true);
if (flag) { return await Task.FromResult("yes"); }
else { return await Task.FromResult("no"); }
