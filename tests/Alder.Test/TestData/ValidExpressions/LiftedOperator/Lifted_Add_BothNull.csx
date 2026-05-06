// §12.4.8: lifted operator — null + null produces null
int? a = null;
int? b = null;
int? result = a + b;
return result.HasValue == false;
