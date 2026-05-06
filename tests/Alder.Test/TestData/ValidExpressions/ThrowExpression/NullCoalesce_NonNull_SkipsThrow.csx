// §12.16: throw expression in null coalescing — non-null skips throw
string s = "hello";
string result = s ?? throw new InvalidOperationException("never");
return result;
