// §10.2: object cannot be implicitly converted to int (unboxing is explicit only, §10.3.7)
object o = 42;
int x = o;
return x;
