Func<int, Task<int>> doubleIt = async n => await Task.FromResult(n * 2);
var sum = 0;
for (var i = 1; i <= 3; i++)
{
    sum += await doubleIt(i);
}
return sum;
