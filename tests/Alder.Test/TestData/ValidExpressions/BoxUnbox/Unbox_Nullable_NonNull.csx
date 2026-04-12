// §10.2.9, §10.3.7: boxing a non-null nullable boxes the wrapped value, then unbox
object o = (int?)5;
return (int)o;
