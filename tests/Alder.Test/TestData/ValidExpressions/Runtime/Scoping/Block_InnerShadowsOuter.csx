// §13.3: block scoping — inner scope accesses outer, variable not visible outside
int x = 10;
int result = 0;
if (true)
{
    int y = 20;
    result = x + y;
}
return result;
