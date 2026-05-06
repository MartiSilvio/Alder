// §10.2.8: no implicit reference conversion from object[] to int[] (int is a value type)
object[] o = new object[] { 1, 2 };
int[] i = o;
return i;
