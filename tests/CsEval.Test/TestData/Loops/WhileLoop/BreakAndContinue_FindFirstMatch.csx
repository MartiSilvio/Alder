var data = new int[] { 1, -2, 3, -4, 5, -6, 7 };
var i = 0;
var firstPositiveAfterNegative = -1;
var sawNegative = false;
while (i < 7) {
    var val = data[i];
    i = i + 1;
    if (val < 0) { sawNegative = true; continue; }
    if (sawNegative) { firstPositiveAfterNegative = val; break; }
}
return firstPositiveAfterNegative;
