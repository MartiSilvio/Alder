// §13.8.3: multiple case labels stacked on one body
int n = 2;
string result = "";
switch (n)
{
    case 1:
    case 2:
    case 3:
        result = "small";
        break;
    case 4:
    case 5:
        result = "medium";
        break;
    default:
        result = "other";
        break;
}
return result;
