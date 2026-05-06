// §15.15: await inside a for loop body, accumulator
int sum = 0;
for (int i = 0; i < 4; i++)
{
    sum += await Task.FromResult(i);
}
return sum;
