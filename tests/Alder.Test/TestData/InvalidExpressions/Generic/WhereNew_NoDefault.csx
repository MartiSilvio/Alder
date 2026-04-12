// §8.4.5: new() constraint violated — string has no parameterless ctor
T F<T>() where T : new() => new T();
return F<string>();
