// §12.4.8: lifted operator — null + value produces null
int? a = null;
int? b = 5;
int? result = a + b;
return result.HasValue == false;
