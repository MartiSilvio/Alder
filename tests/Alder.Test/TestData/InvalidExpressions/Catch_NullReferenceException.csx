try { ((string)null).Length; }
catch (NullReferenceException) { return "caught"; }
return "missed";