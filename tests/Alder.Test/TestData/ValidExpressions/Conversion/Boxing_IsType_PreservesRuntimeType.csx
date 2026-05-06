// §10.2.9: boxing preserves runtime type — boxed int is still int
object o = 42;
return o is int;
