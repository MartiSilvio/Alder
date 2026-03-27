object x = "hello";
var isStr = x is string;
var asStr = x as string;
var cast = (string)x;
return isStr.ToString() + "|" + asStr + "|" + cast;
