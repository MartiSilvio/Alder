var numbers = new System.Collections.Generic.List<int>();
for (var i = 1; i <= 10; i++) {
    numbers.Add(i);
}
return System.Linq.Enumerable.Count(System.Linq.Enumerable.Where(numbers, x => x % 2 == 0));
