// §12.9.7: cast expression — reference conversion from string to object
string s = "hello";
object o = (object)s;
return o is string;
