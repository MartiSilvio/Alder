var last = 0;
foreach (var i in new[] { 1, 2, 3 }) {
    var x = i * 10;
    last = x;
}
return last;
