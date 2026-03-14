try { throw new System.Exception("test"); }
catch (System.Exception ex) { return ex.StackTrace != null; }
