var a = (string)null ?? "default";
var b = "exists" ?? "fallback";
string c = null;
c ??= "assigned";
var d = "hello"?.Length;
return a + "|" + b + "|" + c + "|" + d;
