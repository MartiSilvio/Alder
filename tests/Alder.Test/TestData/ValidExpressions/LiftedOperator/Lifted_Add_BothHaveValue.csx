// §12.4.8: lifted operator — both non-null produces sum
int? a = 3;
int? b = 4;
int? result = a + b;
return result.Value;
