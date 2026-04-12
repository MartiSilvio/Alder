// §10.2.3: no implicit conversion from short to ulong (short can be negative)
short s = 42;
ulong u = s;
return u;
