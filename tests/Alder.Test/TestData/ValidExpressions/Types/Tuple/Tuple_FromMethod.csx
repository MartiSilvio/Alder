// §13.6.4 + §8.3.11: local function returning tuple
(int min, int max) MinMax(int a, int b) => a < b ? (a, b) : (b, a);
var r = MinMax(7, 3);
return r.min + r.max;
