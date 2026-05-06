// §12.8.19: unchecked uint-to-int reinterprets high bit as sign
uint u = uint.MaxValue;
return unchecked((int)u) == -1;
