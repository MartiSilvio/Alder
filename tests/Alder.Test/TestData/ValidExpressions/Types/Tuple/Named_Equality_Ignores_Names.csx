// §12.12.11: tuple equality ignores element names
var a = (x: 1, y: 2);
var b = (a: 1, b: 2);
return a == b;
