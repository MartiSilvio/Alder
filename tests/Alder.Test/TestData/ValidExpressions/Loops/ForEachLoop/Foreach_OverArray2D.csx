int[,] grid = new int[,] { { 1, 2 }, { 3, 4 } };
int sum = 0;
foreach (int n in grid)
{
    sum += n;
}
return sum;
