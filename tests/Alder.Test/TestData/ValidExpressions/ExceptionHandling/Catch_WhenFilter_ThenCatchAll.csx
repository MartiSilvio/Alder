try { throw new Exception("x"); }
catch when (false) { return "filtered"; }
catch { return "caught"; }