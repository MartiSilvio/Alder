// §13.8.3: relational pattern in case label
int n = 15;
string result = "";
switch (n)
{
    case < 0:
        result = "neg";
        break;
    case 0:
        result = "zero";
        break;
    case > 10:
        result = "big";
        break;
    default:
        result = "small";
        break;
}
return result;
