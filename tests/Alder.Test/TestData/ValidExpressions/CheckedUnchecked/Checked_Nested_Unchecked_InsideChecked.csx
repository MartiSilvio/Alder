// §12.8.19: unchecked inside checked suppresses overflow only in inner scope
int outer = int.MaxValue;
int inner = checked(unchecked(outer + 1) + 0);
return inner == int.MinValue;
