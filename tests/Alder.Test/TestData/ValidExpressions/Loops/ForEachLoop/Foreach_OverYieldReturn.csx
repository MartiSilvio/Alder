IEnumerable<int> Gen()
{
    yield return 1;
    yield return 2;
    yield return 3;
}
int sum = 0;
foreach (int n in Gen())
{
    sum += n;
}
return sum;
