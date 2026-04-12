// §12.8.19: unchecked arithmetic wraps int.MaxValue + 1 to int.MinValue
return unchecked(int.MaxValue + 1) == int.MinValue;
