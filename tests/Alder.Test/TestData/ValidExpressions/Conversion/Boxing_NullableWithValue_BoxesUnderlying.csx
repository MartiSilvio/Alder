// §10.2.9: boxing nullable with value — boxes underlying value
int? n = 42;
object o = n;
return o is int && (int)o == 42;
