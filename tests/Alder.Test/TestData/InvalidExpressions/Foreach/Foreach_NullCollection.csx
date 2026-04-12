// §13.9.5: foreach on null throws NullReferenceException at runtime
string[] arr = null;
int count = 0;
foreach (var item in arr)
    count++;
return count;
