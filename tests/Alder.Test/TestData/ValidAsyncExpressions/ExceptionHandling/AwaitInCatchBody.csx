try { throw new InvalidOperationException(); }
catch { return await Task.FromResult("caught"); }
