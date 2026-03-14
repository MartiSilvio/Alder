try { throw new System.InvalidOperationException(); }
catch (System.InvalidOperationException ex) { return ex.HResult != 0; }
