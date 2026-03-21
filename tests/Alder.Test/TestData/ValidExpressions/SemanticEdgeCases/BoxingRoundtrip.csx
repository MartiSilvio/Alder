// Verifies ECMA-334 section 8.3.13: Boxing int to object, then unboxing back
{ object boxed = 42; return (int)boxed; }