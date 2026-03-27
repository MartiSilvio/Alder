var a = new[] { 1, 2, 3, 4 }.Contains(3);
var b = !new[] { 1, 2, 3, 4 }.Contains(5);
return a && b;
