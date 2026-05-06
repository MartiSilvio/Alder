// §12.8.3: conditional expression inside interpolation hole
var x = 10;
return $"x is {(x > 5 ? "big" : "small")}";
