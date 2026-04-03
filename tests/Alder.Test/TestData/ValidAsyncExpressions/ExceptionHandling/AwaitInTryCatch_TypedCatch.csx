try { throw new ArgumentException("test"); }
catch (ArgumentException ex) { return await Task.FromResult(ex.Message); }
