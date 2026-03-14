var x = 1;
var result = 0;
switch (x)
{
    case 1:
        result = 10;
        goto case 2;
    case 2:
        result = result + 20;
        break;
    default:
        result = -1;
        break;
}
return result;
