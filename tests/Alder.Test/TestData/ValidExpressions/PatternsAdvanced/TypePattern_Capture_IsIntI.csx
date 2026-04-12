// §11.2: type pattern with variable capture
object o = 42;
if (o is int i)
    return i * 3;
return -1;
