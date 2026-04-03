var flag = await Task.FromResult(false);
return flag ? await Task.FromResult("yes") : await Task.FromResult("no");
