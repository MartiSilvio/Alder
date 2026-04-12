// §13.10.2: break in nested loops — only exits innermost
int count = 0;
for (int i = 0; i < 3; i++)
{
    for (int j = 0; j < 3; j++)
    {
        if (j == 1) break;
        count++;
    }
}
return count;
