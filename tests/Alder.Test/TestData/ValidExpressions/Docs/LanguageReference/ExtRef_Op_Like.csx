var a = "hello" like "hel%";
var b = "hello" like "h_llo";
var c = "hello" not like "xyz%";
return a and b and c;
