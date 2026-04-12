// §11.2: type pattern combined with && boolean check
object o = "hello";
return o is string s && s.Length > 0;
