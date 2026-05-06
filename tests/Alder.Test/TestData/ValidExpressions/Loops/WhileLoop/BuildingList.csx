var result = new List<int>();
var i = 0;
while (i < 5) {
    result.Add(i * 2);
    i = i + 1;
}
return result.ToArray();
