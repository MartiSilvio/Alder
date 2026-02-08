var count = 0;
var i = 0;
while (i < 10) {
    i = i + 1;
    if (i < 3) { continue; }
    if (i > 7) { continue; }
    count = count + 1;
}
return count;
