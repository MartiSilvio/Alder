// §15.15: await inside a while loop with counter exit
int count = 0;
while (count < 3)
{
    count += await Task.FromResult(1);
}
return count;
