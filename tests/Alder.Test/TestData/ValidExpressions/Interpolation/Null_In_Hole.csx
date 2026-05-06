// §12.8.3: null value in interpolation hole produces empty string
string? s = null;
return $"[{s}]";
