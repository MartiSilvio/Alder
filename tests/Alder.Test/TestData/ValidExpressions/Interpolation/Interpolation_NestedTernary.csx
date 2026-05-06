// §12.8.3: interpolated string — ternary inside interpolation hole
int x = 5;
return $"result: {(x > 3 ? "big" : "small")}";
