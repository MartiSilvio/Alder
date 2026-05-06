// §10.3.7: unboxing null to nullable — produces null value
object o = null;
int? n = (int?)o;
return n.HasValue == false;
