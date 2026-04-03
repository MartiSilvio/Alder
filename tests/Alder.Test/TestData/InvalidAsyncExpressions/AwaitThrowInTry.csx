try { throw new InvalidOperationException(await Task.FromResult("boom")); }
catch (ArgumentException) { return "wrong catch"; }
