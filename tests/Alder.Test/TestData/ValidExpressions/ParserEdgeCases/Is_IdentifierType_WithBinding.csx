object x = new ArgumentException("bad param");
return x is ArgumentException ex ? ex.Message : "none";
