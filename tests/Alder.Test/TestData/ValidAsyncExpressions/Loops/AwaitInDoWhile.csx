var count = 0;
do
{
    count += await Task.FromResult(1);
} while (count < 3);
return count;
