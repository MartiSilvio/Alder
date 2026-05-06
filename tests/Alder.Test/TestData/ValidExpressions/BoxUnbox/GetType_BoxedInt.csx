// §8.3.13: GetType on a boxed int returns typeof(int)
object o = 42;
return o.GetType() == typeof(int);
