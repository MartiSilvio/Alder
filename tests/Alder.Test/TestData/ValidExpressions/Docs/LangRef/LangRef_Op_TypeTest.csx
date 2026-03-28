var obj = (object)"hello";
var a = obj is string;
var b = obj is string s ? s.Length : 0;
var c = obj as string;
return a && b == 5 && c == "hello";
