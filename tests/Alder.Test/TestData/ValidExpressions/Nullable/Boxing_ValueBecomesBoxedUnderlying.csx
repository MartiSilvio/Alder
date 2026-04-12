// §10.2.9: Boxing a non-null nullable boxes the underlying value.
object o = (int?)5;
return (int)o == 5;
