// §13.9.5: foreach requires IEnumerable — int is not enumerable
int x = 42;
int sum = 0;
foreach (var item in x)
    sum += item;
return sum;
