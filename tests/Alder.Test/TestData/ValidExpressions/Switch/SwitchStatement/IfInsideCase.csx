var x = 1;
var val = 15;
var category = "";
switch (x) {
    case 1:
        if (val > 10) {
            category = "high";
        } else {
            category = "low";
        }
        break;
}
return category;
