// §12.13.5: nullable bool AND — false & null is false
bool? a = false;
bool? b = null;
bool? result = a & b;
return result.HasValue && result.Value == false;
