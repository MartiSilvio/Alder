// §10.3.2: "From short to sbyte, byte, ushort, uint, ulong, or char" — explicit narrowing short to sbyte
short s = 100;
return (sbyte)s == 100;
