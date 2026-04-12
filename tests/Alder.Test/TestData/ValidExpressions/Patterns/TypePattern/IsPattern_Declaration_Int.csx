// §12.12.12: is-pattern with declaration — type pattern with variable binding
object o = 42;
if (o is int x)
    return x * 2;
return -1;
