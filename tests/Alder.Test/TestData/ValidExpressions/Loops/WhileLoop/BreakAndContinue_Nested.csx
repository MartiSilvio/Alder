var total = 0;
var i = 0;
while (i < 5) {
    i = i + 1;
    if (i == 3) { continue; }
    var j = 0;
    while (j < 5) {
        j = j + 1;
        if (j == 2) { break; }
        total = total + 1;
    }
}
return total;
