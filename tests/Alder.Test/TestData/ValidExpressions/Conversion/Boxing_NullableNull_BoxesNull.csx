// §10.2.9: boxing nullable null — produces null reference
int? n = null;
object o = n;
return o == null;
