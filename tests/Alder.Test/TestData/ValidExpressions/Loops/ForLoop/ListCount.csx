var items = new List<int> { 10, 20, 30, 40 };
var sum = 0;
for (var i = 0; i < items.Count; i++) {
    sum += items[i];
}
return sum;
