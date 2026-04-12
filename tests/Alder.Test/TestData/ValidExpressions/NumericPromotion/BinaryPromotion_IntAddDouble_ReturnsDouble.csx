// §12.4.7.3: binary numeric promotion — int + double promotes to double
int a = 3;
double b = 0.14;
var result = a + b;
return result.GetType() == typeof(double);
