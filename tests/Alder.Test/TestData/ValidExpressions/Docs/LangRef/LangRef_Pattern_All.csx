var obj = (object)42;
var a = obj is int;
var b = obj is int n && n > 0;
var c = obj is > 0 and < 100;
var d = obj is not null;
var e = obj is var v;
var arr = new[] { 1, 2, 3 };
var f = arr is [1, 2, 3];
return a && b && c && d && e && f;
