// Known limitation: typed tuple deconstruction `(int a, int b) = (...)` is not supported by the
// parser — only `var (a, b) = ...` is recognized.
// §12.21.2: deconstruction with explicit element types
(int a, int b) = (10, 20);
return a + b;
