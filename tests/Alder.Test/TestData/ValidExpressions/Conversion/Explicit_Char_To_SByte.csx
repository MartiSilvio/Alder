// §10.3.2: "From char to sbyte, byte, or short" — explicit narrowing char to sbyte
char c = (char)65;
return (sbyte)c == 65;
