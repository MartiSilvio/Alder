// §12.17: declaration expression — out var on failed parse
bool success = int.TryParse("abc", out var n);
return success == false && n == 0;
