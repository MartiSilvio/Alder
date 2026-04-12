// Duplicate case labels in switch statement
int x = 1;
switch (x)
{
    case 1: return "a";
    case 1: return "b";
    default: return "c";
}
