{
    var fruit = "Apple";
    var category = "";
    switch (fruit) {
        case "apple":
            category = "lowercase";
            break;
        case "Apple":
            category = "capitalized";
            break;
        default:
            category = "unknown";
            break;
    }
    return category;
}
