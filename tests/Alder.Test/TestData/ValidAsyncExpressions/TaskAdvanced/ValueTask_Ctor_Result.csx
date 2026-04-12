await Task.CompletedTask;
ValueTask<int> vt = new ValueTask<int>(42);
return vt.Result;
