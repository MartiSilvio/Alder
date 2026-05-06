try { throw new Exception("x"); }
catch when (false) { return "filtered"; }
catch (Exception) { return "caught"; }