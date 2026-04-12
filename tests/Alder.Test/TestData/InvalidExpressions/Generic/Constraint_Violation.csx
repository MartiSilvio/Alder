// CS0453: The type 'string' must be a non-nullable value type
T Identity<T>(T x) where T : struct => x;
return Identity<string>("hello");
