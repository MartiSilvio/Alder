// §10.6.1: explicit nullable conversion from T? to T throws InvalidOperationException when HasValue is false
int? x = null;
return (int)x;
