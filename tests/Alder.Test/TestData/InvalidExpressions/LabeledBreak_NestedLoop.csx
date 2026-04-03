var x = 0;
outer: for (var i = 0; i < 3; i++) {
    for (var j = 0; j < 3; j++) {
        if (j == 1) break outer;
        x++;
    }
}
x