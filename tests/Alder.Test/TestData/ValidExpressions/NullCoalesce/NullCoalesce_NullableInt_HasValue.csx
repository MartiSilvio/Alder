// §12.15: null coalescing with nullable int — returns value when has value
int? n = 42;
int result = n ?? 0;
return result;
