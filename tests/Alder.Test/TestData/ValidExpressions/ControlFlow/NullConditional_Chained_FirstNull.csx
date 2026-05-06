// §12.8.8: null-conditional member access — first receiver is null, short-circuits
string s = null;
return s?.ToUpper()?.Length;
