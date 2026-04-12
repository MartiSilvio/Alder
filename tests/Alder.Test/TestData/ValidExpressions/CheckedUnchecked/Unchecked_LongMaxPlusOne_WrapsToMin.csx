// §12.8.19: unchecked long overflow wraps
return unchecked(long.MaxValue + 1L) == long.MinValue;
