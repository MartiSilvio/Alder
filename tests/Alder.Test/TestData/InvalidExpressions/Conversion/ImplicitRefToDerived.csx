// §10.2.8: downcast from object to string is not an implicit reference conversion (requires explicit cast, §10.3.5)
object o = "hello";
string s = o;
return s;
