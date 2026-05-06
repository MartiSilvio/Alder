// §13.8.3: nested switch statements
int outer = 1;
int inner = 2;
int result = 0;
switch (outer)
{
    case 1:
        switch (inner)
        {
            case 1:
                result = 11;
                break;
            case 2:
                result = 12;
                break;
        }
        break;
    default:
        result = -1;
        break;
}
return result;
