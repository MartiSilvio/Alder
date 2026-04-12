// §12.15: null coalescing with nullable int — returns right when null
int? n = null;
int result = n ?? -1;
return result;
