// §10.3.2: "From sbyte to byte, ushort, uint, ulong, or char" — explicit sbyte to char
sbyte s = 65;
return (char)s == 'A';
