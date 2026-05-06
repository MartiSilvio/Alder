// §12.20: simple from...select query
var list = new[] { 1, 2, 3, 4 };
return (from x in list select x).Sum();
