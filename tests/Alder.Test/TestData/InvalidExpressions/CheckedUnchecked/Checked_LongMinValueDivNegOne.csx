// §12.10.3: long.MinValue / -1 overflows in checked context
long v = long.MinValue;
long d = -1L;
return checked(v / d);
