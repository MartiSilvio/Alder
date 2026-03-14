var x = 1;
var result = 0;
switch (x)
{
    case 1:
        result = 10;
        goto default;
    case 2:
        result = 20;
        break;
    default:
        result = result + 100;
        break;
}
return result;
