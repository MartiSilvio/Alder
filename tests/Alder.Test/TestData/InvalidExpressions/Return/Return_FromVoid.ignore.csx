// Known limitation: local function bodies are not bound against their declared return type,
// so `void Foo() { return 42; }` is silently accepted instead of throwing CS0127.
// Local function with void return type cannot return a value
void NoReturn() { return 42; }
NoReturn();
return 0;
