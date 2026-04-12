// §10.2.3: no implicit conversion from char to sbyte (char is 16-bit unsigned)
char c = 'A';
sbyte s = c;
return s;
