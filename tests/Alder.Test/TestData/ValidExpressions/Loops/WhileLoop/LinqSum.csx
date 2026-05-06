var numbers = new List<int>();
var i = 1;
while (i <= 5) {
    numbers.Add(i);
    i = i + 1;
}
return Enumerable.Sum(numbers);
