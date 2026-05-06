// §12.4.8: lifted unary — negating non-null produces negated value
int? a = 5;
int? result = -a;
return result.Value;
