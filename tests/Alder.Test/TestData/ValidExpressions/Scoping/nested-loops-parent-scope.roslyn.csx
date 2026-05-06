var total = 0;
for (var i = 1; i <= 3; i = i + 1) {
    foreach (var j in new[] { 10, 20 }) {
        var k = 0;
        while (k < 2) {
            total = total + i + j + k;
            k = k + 1;
        }
    }
}
return total;
