// §10.2.9: Interface method on value type via boxing
IFormattable f = 42;
return f.ToString("D", null);
