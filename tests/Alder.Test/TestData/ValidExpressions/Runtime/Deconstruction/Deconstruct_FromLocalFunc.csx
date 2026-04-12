// §13.6.4 + §12.21.2: local function returning tuple is deconstructed at call site
(int, int) GetPair() => (5, 7);
var (x, y) = GetPair();
return x * y;
