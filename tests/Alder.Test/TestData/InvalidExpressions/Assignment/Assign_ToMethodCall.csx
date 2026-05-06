// §12.21.2: cannot assign to a method call result (not an lvalue)
string s = "hello";
s.ToUpper() = "HELLO";
return s;
