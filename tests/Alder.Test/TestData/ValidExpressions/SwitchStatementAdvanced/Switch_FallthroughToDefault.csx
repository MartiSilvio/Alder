// §13.8.3: unmatched case falls to default
int n = 99;
int result = 0;
switch (n)
{
    case 1:
        result = 10;
        break;
    case 2:
        result = 20;
        break;
    default:
        result = -1;
        break;
}
return result;
