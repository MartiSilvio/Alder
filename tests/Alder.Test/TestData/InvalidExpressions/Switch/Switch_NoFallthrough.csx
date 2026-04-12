// §13.8.3: C# does not allow switch case fall-through (CS0163)
int x = 1;
switch (x)
{
    case 1:
        var y = 5;
    case 2:
        return 10;
    default:
        return 0;
}
