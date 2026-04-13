// Known limitation: generic type parameters and `where` constraints on local functions are not supported.
// §8.4.5: struct constraint violated — string is a reference type
T F<T>() where T : struct => default(T);
return F<string>();
