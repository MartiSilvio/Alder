// §13.8.3: goto default
int n = 5;
string result = "";
switch (n)
{
    case 1:
        result = "one";
        break;
    case 5:
        goto default;
    default:
        result = "def";
        break;
}
return result;
