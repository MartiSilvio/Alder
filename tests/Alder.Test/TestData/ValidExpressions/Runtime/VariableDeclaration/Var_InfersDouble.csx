// §13.6.2.2: implicitly typed local variable — infers double from literal
var d = 3.14;
return d.GetType() == typeof(double);
