// §8.4.5: struct constraint violated — string is a reference type
T F<T>() where T : struct => default(T);
return F<string>();
