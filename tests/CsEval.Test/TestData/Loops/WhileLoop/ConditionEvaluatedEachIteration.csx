var limit = 5;
var i = 0;
var sum = 0;
while (i < limit) {
    sum = sum + i;
    i = i + 1;
    if (i == 3) { limit = 3; }
}
return sum;
