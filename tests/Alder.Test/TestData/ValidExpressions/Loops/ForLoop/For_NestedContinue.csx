// §13.10.3: continue in nested loops — only skips innermost iteration
int count = 0;
for (int i = 0; i < 3; i++)
{
    for (int j = 0; j < 3; j++)
    {
        if (j == 1) continue;
        count++;
    }
}
return count;
