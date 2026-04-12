// §10.2.9: Boxing a null nullable produces a null reference.
object o = (int?)null;
return o == null;
