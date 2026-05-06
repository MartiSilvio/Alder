var sum = 0;
for (var i = 0; i < 5; i++)
{
    if (i == 3) continue;
    if (i == 4) break;
    for (var j = 0; j < 3; j++)
    {
        if (j == 1) break;
        sum++;
    }
}
return sum;
