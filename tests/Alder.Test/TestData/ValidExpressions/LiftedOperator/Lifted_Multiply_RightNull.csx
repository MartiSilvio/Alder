// §12.4.8: lifted operator — value * null produces null
int? a = 5;
int? b = null;
int? result = a * b;
return result.HasValue == false;
