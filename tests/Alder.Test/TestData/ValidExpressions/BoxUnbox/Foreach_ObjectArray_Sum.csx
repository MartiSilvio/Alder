// §13.9.5: foreach over object[] with explicit unboxing
object[] arr = new object[] { 1, 2, 3 };
int sum = 0;
foreach (var o in arr)
    sum += (int)o;
return sum;
