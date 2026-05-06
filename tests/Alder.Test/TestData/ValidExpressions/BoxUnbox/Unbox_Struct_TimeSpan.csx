// §10.3.7: unboxing a value-type struct preserves field data
object o = TimeSpan.FromSeconds(1);
return ((TimeSpan)o).TotalSeconds;
