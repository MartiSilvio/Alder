// §12.8.3: interpolated string containing another interpolated string in a hole
int x = 7;
string inner = $"<{x}>";
return $"[{inner}]";
