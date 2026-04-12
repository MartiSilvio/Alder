// §10.2.6: implicit nullable conversion — int to int?
int? n = 42;
return n.HasValue && n.Value == 42;
