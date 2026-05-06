// §10.2.9/§10.3.7: boxing then unboxing preserves value
int original = 12345;
object boxed = original;
int unboxed = (int)boxed;
return original == unboxed;
