// §13.8.3: switch statement on double constant
double d = 1.5;
string result = "";
switch (d)
{
    case 0.0:
        result = "zero";
        break;
    case 1.5:
        result = "one-five";
        break;
    default:
        result = "other";
        break;
}
return result;
