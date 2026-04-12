// §13.9.3: do-while — body executes at least once even if condition is false
int x = 0;
do
{
    x++;
}
while (false);
return x;
