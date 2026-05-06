var outer = 1;
if (true) {
    var middle = 2;
    if (true) {
        var inner = 3;
        outer = outer + middle + inner;
    }
}
return outer;
