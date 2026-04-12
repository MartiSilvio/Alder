// §12.4.7.3: binary numeric promotion — short - short promotes to int
short a = 100;
short b = 50;
var result = a - b;
return result.GetType() == typeof(int);
