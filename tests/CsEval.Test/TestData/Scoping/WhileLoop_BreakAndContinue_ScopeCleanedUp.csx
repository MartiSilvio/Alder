{
    var total = 0;
    var i = 0;
    while (true) {
        var x = i * 2;
        i = i + 1;
        if (i == 3) {
            continue;
        }
        if (i > 5) {
            break;
        }
        total = total + x;
    }
    return total;
}
