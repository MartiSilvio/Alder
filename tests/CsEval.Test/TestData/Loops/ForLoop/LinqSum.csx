var numbers = new System.Collections.Generic.List<int>();
for (var i = 1; i <= 5; i++) {
    numbers.Add(i);
}
return System.Linq.Enumerable.Sum(numbers);
