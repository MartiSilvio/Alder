// §12.7: nested deconstruction
var (a, (b, c)) = (1, (2, 3));
return a + b + c;
