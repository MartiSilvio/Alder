// §10.2.11: "A constant_expression of type long can be converted to type ulong, provided the value of the constant_expression is not negative" — negative value fails
ulong u = -1L;
return u;
