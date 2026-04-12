var numbers = new List<int>();
var i = 1;
while (i <= 10) {
    numbers.Add(i);
    i = i + 1;
}
return Enumerable.Count(Enumerable.Where(numbers, x => x % 2 == 0));
