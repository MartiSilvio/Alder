// §13.9.5: nested foreach over iterators
var total = 0;
foreach (var a in Enumerable.Range(1, 3))
    foreach (var b in Enumerable.Range(1, 3))
        total += a * b;
return total;
