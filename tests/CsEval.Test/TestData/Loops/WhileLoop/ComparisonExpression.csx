var numbers = new int[] { 10, 5, 15, 3 };
var idx = 0;
var found = -1;
while (idx < 4 && found == -1) {
    if (numbers[idx] == 15) {
        found = idx;
    }
    idx++;
}
return found;
