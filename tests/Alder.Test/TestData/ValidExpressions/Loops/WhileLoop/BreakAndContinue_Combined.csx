var sum = 0;
var i = 0;
while (true) {
    i = i + 1;
    if (i % 2 == 0) { continue; }
    if (i > 10) { break; }
    sum = sum + i;
}
return sum;
