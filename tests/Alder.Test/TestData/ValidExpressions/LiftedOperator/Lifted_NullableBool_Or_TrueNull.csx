// §12.13.5: nullable bool OR — true | null is true
bool? a = true;
bool? b = null;
bool? result = a | b;
return result.HasValue && result.Value == true;
