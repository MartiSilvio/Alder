{
    var x = 2;
    var result = "";
    switch (x) {
        case 1:
        case 2:
        case 3:
            result = "1, 2, or 3";
            break;
        default:
            result = "other";
            break;
    }
    return result;
}
