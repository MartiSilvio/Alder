// §12.12.12: is-pattern with declaration — string type pattern
object o = "hello";
if (o is string s)
    return s.Length;
return -1;
