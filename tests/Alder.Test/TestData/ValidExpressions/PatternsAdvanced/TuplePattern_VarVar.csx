// §11.2: tuple positional with var captures
var t = (10, 20);
if (t is (var x, var y))
    return x + y;
return -1;
