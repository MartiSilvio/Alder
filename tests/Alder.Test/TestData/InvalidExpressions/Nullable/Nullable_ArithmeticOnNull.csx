// §12.4.8: lifted operator on null yields null, but assigning null result to non-nullable fails
int? a = null;
int? b = null;
int result = a + b;
return result;
