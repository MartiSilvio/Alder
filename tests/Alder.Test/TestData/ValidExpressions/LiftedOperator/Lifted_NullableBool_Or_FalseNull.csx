// §12.13.5: nullable bool OR — false | null is null
bool? a = false;
bool? b = null;
bool? result = a | b;
return result.HasValue == false;
