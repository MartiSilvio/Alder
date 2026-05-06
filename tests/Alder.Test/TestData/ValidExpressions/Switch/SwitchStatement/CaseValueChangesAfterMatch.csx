var x = 1;
var count = 0;
switch (x) {
    case 1:
        x = 2;
        count = count + 1;
        break;
    case 2:
        count = count + 10;
        break;
}
return count;
