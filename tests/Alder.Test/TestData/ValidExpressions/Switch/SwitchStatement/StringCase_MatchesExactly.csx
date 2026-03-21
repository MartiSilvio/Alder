var fruit = "apple";
var category = "";
switch (fruit) {
    case "apple":
        category = "pome";
        break;
    case "orange":
        category = "citrus";
        break;
    case "banana":
        category = "tropical";
        break;
    default:
        category = "unknown";
        break;
}
return category;
