// Known limitation: nested deconstruction patterns `var (a, (b, c)) = ...` are not supported
// by the parser.
// §12.7: nested deconstruction
var (a, (b, c)) = (1, (2, 3));
return a + b + c;
