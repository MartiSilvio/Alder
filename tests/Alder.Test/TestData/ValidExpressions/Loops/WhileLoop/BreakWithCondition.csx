var target = 42;
var arr = new int[] { 10, 20, 42, 50, 60 };
var i = 0;
var foundIndex = -1;
while (i < 5) {
    if (arr[i] == target) { foundIndex = i; break; }
    i++;
}
return foundIndex;
