// Known limitation: local function bodies are not bound against their declared return type,
// so `int Foo() { return "hello"; }` is silently accepted instead of throwing CS0029.
// Local function with return type mismatch
int GetValue() { return "hello"; }
return GetValue();
