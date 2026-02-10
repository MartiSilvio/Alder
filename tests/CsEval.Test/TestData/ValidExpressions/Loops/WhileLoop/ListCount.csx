var items = new System.Collections.Generic.List<int> { 10, 20, 30, 40 };
var sum = 0;
var i = 0;
while (i < items.Count) {
    sum = sum + items[i];
    i = i + 1;
}
return sum;
