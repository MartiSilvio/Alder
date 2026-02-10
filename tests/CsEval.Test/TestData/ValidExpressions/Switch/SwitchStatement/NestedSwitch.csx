{
    var outer = 1;
    var inner = 2;
    var result = "";
    switch (outer) {
        case 1:
            switch (inner) {
                case 1:
                    result = "1-1";
                    break;
                case 2:
                    result = "1-2";
                    break;
            }
            break;
        case 2:
            result = "2-x";
            break;
    }
    return result;
}
