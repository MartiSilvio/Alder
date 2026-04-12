// §12.12.13: as operator — type mismatch returns null
object o = 42;
string s = o as string;
return s == null;
