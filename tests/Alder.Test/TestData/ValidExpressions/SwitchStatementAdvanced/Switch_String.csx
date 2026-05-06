// §13.8.3: switch statement on string
string s = "beta";
int result = 0;
switch (s)
{
    case "alpha":
        result = 1;
        break;
    case "beta":
        result = 2;
        break;
    case "gamma":
        result = 3;
        break;
    default:
        result = 0;
        break;
}
return result;
