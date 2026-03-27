var a = System.Text.RegularExpressions.Regex.IsMatch("hello123", @"\d+");
var b = !System.Text.RegularExpressions.Regex.IsMatch("hello", @"\d+");
return a && b;
