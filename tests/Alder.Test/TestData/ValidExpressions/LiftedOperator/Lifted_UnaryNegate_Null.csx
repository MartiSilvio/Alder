// §12.4.8: lifted unary — negating null produces null
int? a = null;
int? result = -a;
return result.HasValue == false;
