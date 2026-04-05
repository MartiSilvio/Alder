var sum = 0;
for (var i = 0; i < 5; i++)
{
    if (i == 2) continue;
    sum += i;
}
return sum;
