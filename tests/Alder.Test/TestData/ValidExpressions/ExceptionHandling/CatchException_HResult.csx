try { throw new InvalidOperationException(); }
catch (InvalidOperationException ex) { return ex.HResult != 0; }
