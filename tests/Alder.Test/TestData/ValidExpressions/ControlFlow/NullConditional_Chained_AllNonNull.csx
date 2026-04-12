// §12.8.8: null-conditional member access — chained ?. on non-null values
string s = "hello";
return s?.ToUpper()?.Length;
