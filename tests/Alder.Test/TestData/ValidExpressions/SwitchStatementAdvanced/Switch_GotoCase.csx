// §13.8.3: goto case forwarding
int n = 1;
int result = 0;
switch (n)
{
    case 1:
        result += 1;
        goto case 2;
    case 2:
        result += 10;
        break;
    default:
        result = -1;
        break;
}
return result;
