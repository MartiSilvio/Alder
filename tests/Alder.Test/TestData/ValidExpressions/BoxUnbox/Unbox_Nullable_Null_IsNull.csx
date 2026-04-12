// §8.3.12: boxing a null nullable yields a null reference
object o = (int?)null;
return o == null;
