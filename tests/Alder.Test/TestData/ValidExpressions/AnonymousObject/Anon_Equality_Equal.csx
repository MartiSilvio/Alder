// §12.8.16.7: two anonymous objects with equal members compare equal via Equals
var a = new { X = 1, Y = 2 };
var b = new { X = 1, Y = 2 };
return a.Equals(b);
