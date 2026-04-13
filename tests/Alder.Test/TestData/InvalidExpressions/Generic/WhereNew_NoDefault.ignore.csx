// Known limitation: generic type parameters and `where` constraints on local functions are not supported.
// §8.4.5: new() constraint violated — string has no parameterless ctor
T F<T>() where T : new() => new T();
return F<string>();
