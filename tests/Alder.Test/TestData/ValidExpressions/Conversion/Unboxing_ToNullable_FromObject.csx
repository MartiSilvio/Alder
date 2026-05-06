// §10.3.7: unboxing to nullable — object containing int unboxes to int?
object o = 42;
int? n = (int?)o;
return n.HasValue && n.Value == 42;
