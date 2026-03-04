var sum = 0;
var i = 0;
while (i < 10) {
    sum = sum + i;
    if (i == 4) {
        break;
    }
    i = i + 1;
}
return sum;
