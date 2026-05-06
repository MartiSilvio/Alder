// §10.3.2: "From sbyte to byte, ushort, uint, ulong, or char" — explicit narrowing sbyte to byte
sbyte s = 65;
return (byte)s == 65;
