var numbers = new List<int>();
for (var i = 1; i <= 10; i++) {
    numbers.Add(i);
}
return Enumerable.Count(Enumerable.Where(numbers, x => x % 2 == 0));
