// §13.15: yield break — terminates iteration early
IEnumerable<int> TakeWhilePositive(List<int> items)
{
    foreach (var item in items)
    {
        if (item < 0)
            yield break;
        yield return item;
    }
}
var list = new List<int> { 1, 2, 3, -1, 5 };
int count = 0;
foreach (var n in TakeWhilePositive(list))
    count++;
return count;
