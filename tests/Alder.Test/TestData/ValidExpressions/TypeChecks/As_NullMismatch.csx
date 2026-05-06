// §12.12.13: 'as' operator returns null on mismatch
object x = 42;
string s = x as string;
return s == null;
