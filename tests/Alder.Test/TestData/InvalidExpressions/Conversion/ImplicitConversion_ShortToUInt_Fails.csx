// §10.2.3: no implicit conversion from short to uint (short can be negative)
short s = 42;
uint u = s;
return u;
