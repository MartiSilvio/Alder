int count = 0;
for (int i = 0; i < 3; i++)
{
    for (int j = 0; j < 5; j++)
    {
        if (j % 2 == 0) continue;
        count++;
    }
}
return count;
