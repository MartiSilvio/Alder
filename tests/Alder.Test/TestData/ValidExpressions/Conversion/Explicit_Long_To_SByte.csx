// §10.3.2: "From long to sbyte, byte, short, ushort, int, uint, ulong, or char" — explicit narrowing long to sbyte
long l = 100;
return (sbyte)l == 100;
