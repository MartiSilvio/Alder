bool ok = long.TryParse("9999999999", out long result);
return ok && result == 9999999999L;
