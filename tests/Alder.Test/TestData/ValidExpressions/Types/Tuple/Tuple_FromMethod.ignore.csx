// Known limitation: tuple return types on local functions — named-tuple member access on the
// call result loses its element names and `r.min` resolves to the runtime default.
// §13.6.4 + §8.3.11: local function returning tuple
(int min, int max) MinMax(int a, int b) => a < b ? (a, b) : (b, a);
var r = MinMax(7, 3);
return r.min + r.max;
