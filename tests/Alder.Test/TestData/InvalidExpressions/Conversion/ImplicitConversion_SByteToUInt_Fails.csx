// §10.2.3: no implicit conversion from sbyte to uint (sbyte can be negative)
sbyte s = 42;
uint u = s;
return u;
