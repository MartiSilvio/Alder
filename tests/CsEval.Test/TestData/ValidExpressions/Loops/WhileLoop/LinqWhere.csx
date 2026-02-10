var numbers = new System.Collections.Generic.List<int>();
var i = 1;
while (i <= 10) {
    numbers.Add(i);
    i = i + 1;
}
return System.Linq.Enumerable.Count(System.Linq.Enumerable.Where(numbers, x => x % 2 == 0));
