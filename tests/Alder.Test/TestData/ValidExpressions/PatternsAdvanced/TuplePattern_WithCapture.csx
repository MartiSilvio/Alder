// §11.2: positional tuple pattern with captures
var t = (3, 4);
if (t is (int a, int b))
    return a + b;
return -1;
