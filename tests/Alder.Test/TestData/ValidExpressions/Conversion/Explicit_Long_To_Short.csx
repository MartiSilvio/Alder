// §10.3.2: "From long to sbyte, byte, short, ushort, int, uint, ulong, or char" — explicit narrowing long to short
long l = 1000;
return (short)l == 1000;
