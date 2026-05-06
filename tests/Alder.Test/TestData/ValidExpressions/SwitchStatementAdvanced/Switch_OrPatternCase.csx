// §13.8.3: or pattern in case label
int n = 5;
string result = "";
switch (n)
{
    case 1 or 2 or 3:
        result = "low";
        break;
    case 4 or 5 or 6:
        result = "mid";
        break;
    default:
        result = "high";
        break;
}
return result;
