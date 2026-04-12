// §12.16: throw expression in conditional — true branch returns value, throw not evaluated
int x = true ? 42 : throw new InvalidOperationException("never");
return x;
