{
    var sum = 0;
    foreach (var i in new[] { 1, 2, 3, 4, 5 }) {
        var temp = i;
        if (i % 2 == 0) {
            continue;
        }
        sum = sum + temp;
    }
    return sum;
}
