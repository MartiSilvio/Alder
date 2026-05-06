try { throw new Exception("test"); }
catch (Exception ex) { return ex.StackTrace != null; }
