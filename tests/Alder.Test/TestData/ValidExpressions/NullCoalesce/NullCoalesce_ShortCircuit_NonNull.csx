// §12.15: null coalescing — right operand not evaluated when left is non-null
int x = 0;
string s = "hello";
string result = s ?? (++x).ToString();
return result == "hello" && x == 0;
