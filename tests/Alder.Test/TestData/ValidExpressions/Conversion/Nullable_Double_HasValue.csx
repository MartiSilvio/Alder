// §10.2.6: implicit nullable conversion — double to double?
double? d = 3.14;
return d.HasValue && d.Value == 3.14;
