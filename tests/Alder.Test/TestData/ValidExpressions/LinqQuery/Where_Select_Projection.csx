// §12.20: where clause filters, select projects
var list = new[] { 1, 2, 3, 4, 5 };
return (from x in list where x > 2 select x * 2).Sum();
