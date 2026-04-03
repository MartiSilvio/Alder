lock (new object()) { return await Task.FromResult(1); }
