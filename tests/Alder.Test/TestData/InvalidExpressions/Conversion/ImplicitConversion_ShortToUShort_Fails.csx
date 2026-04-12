// §10.2.3: no implicit conversion from short to ushort (short can be negative)
short s = 42;
ushort u = s;
return u;
