// §12.13.5: nullable bool AND — true & null is null
bool? a = true;
bool? b = null;
bool? result = a & b;
return result.HasValue == false;
