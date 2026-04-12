// §10.2.3: no implicit conversion from sbyte to ulong (sbyte can be negative)
sbyte s = 42;
ulong u = s;
return u;
