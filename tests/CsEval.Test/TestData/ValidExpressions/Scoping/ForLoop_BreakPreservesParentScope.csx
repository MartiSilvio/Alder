{
    var sum = 0;
    for (var i = 0; i < 10; i = i + 1) {
        var temp = i;
        sum = sum + temp;
        if (i == 4) {
            break;
        }
    }
    return sum;
}
