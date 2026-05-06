int i = 0;
int sum = 0;
while (i < 10)
{
    i++;
    if (i % 2 == 0) continue;
    sum += i;
}
return sum;
