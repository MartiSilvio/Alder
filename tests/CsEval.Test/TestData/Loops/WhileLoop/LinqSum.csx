var numbers = new System.Collections.Generic.List<int>();
var i = 1;
while (i <= 5) {
    numbers.Add(i);
    i = i + 1;
}
return System.Linq.Enumerable.Sum(numbers);
