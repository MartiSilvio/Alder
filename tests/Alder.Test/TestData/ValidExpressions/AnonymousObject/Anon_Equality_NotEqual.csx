// §12.8.16.7: anonymous objects with differing member values are not equal
var a = new { X = 1, Y = 2 };
var b = new { X = 1, Y = 99 };
return a.Equals(b);
