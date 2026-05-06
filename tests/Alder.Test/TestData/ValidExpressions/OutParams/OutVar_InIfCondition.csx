// §12.17: declaration expression — out var in if condition
if (int.TryParse("42", out var n))
    return n;
return -1;
