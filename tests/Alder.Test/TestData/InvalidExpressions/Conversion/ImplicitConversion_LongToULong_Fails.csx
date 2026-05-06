// §10.2.3: no implicit conversion from long to ulong (may lose sign)
long l = 42L;
ulong u = l;
return u;
