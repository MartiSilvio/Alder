// §13.9.5: foreach over Enumerable.Range
var total = 0;
foreach (var n in Enumerable.Range(1, 5))
    total += n;
return total;
