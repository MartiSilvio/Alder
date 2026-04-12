// §12.4.7.3: binary numeric promotion — float + double promotes to double
float a = 1.5f;
double b = 2.5;
var result = a + b;
return result.GetType() == typeof(double);
