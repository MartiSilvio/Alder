{
    var x = 3.14;
    var result = "";
    switch (x) {
        case 3.14:
            result = "pi";
            break;
        case 2.71:
            result = "e";
            break;
        default:
            result = "other";
            break;
    }
    return result;
}
