var target = 7;
var i = 0;
while (i < 20) {
    if (i == target) {
        return $"Found at {i}";
    }
    i++;
}
return "Not found";
