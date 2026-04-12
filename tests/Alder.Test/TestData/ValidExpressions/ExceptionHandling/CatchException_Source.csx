try { throw new InvalidOperationException("bad"); }
catch (InvalidOperationException ex) { return ex.Message; }
