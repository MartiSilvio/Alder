var a = "hello".StartsWith("hel");
var b = "hello".Length == 5 && "hello"[0] == 'h' && "hello"[2] == 'l' && "hello"[3] == 'l' && "hello"[4] == 'o';
var c = !("hello".StartsWith("xyz"));
return a && b && c;
