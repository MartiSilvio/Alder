{
    var arr = new[] { 1, 2, 3 };
    var i = 0;
    while (i < 3) {
        arr[i] = arr[i] * 2;
        i++;
    }
    return arr[1];
}
