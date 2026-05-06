// §8.3.13: object.GetType virtual dispatch through boxed value
object o = 42;
return o.GetType().Name == "Int32";
