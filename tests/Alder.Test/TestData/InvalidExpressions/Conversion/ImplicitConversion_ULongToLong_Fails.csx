// §10.2.3: no implicit conversion from ulong to long (narrowing)
ulong u = 42uL;
long l = u;
return l;
