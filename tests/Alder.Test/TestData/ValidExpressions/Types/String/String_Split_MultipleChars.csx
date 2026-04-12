var parts = "a,b;c.d".Split(',', ';', '.');
return string.Join("-", parts);
