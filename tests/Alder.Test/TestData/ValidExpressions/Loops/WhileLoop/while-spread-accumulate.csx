var i = 0;
var results = [];
while (i < 3) {
    var x = i * 10;
    results = [..results, x];
    i = i + 1;
}
return results;
