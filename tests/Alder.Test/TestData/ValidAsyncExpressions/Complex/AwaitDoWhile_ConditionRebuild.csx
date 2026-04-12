// §15.15: do/while with award in both body and condition
int i = 0;
int sum = 0;
do
{
    sum += await Task.FromResult(i);
    i++;
} while (i < 5);
return sum;
