// §13.8.3: constant used in switch case label
const int target = 2;
var v = 2;
switch (v)
{
    case target:
        return 100;
    default:
        return 0;
}
