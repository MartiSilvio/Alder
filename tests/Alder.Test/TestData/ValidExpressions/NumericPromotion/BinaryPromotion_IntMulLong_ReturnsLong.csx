// §12.4.7.3: binary numeric promotion — int * long promotes to long
int a = 5;
long b = 10L;
var result = a * b;
return result.GetType() == typeof(long) && result == 50L;
