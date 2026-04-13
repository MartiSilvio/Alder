// Known limitation: generic type parameters and `where` constraints on local functions are not supported.
// CS0453: The type 'string' must be a non-nullable value type
T Identity<T>(T x) where T : struct => x;
return Identity<string>("hello");
