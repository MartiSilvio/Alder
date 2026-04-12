// §13.8.3: switch statement — multiple case labels for same section
int x = 2;
switch (x)
{
    case 1:
    case 2:
    case 3:
        return "one-to-three";
    default:
        return "other";
}
