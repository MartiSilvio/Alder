// §10.2.6: implicit nullable conversion — bool to bool?
bool? b = true;
return b.HasValue && b.Value;
