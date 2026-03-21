try { throw new System.InvalidOperationException("bad"); }
catch (System.InvalidOperationException ex) { return ex.Message; }
