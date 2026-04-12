// §12.8.8: null-conditional — ?. followed by [] on the result
string s = "hello world";
return s?.Split(' ')?[0];
