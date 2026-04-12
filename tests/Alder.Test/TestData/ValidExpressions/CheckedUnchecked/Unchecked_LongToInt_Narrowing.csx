// §12.8.19: unchecked long-to-int narrowing truncates high bits
long x = (long)int.MaxValue + 1L;
return unchecked((int)x) == int.MinValue;
