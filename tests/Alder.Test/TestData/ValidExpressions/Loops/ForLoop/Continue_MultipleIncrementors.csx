var sum = 0;
for (int i = 0, j = 10; i < 5; i++, j--)
{
    if (i == 2) continue;
    sum += j;
}
return sum;
