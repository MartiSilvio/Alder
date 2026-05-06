// §20.5: Converter<TInput, TOutput> instantiation via lambda
Converter<int, string> conv = i => i.ToString();
return conv(42);
