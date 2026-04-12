// §12.8.16.3: empty object initializer after constructor
var rx = new System.Text.RegularExpressions.Regex("a") { };
return rx.IsMatch("banana");
