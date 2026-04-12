// §13.15: yield return — iterator method produces sequence
IEnumerable<int> Range(int start, int count)
{
    for (int i = 0; i < count; i++)
        yield return start + i;
}
int sum = 0;
foreach (var n in Range(1, 5))
    sum += n;
return sum;
