// §12.17: declaration expression — out var captures parsed value
bool success = int.TryParse("123", out var n);
return success && n == 123;
