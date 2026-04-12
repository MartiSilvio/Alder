var numbers = new List<int>();
for (var i = 1; i <= 5; i++) {
    numbers.Add(i);
}
return Enumerable.Sum(numbers);
