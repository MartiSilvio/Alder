var arr = [38, 27, 43, 3, 9, 82, 10, 64, 15, 51, 29, 77, 1, 99, 44];
var n = arr.Length;

var stackLow = new int[15];
var stackHigh = new int[15];
var top = 0;

stackLow[top] = 0;
stackHigh[top] = n - 1;
top++;

while (top > 0)
{
    top--;
    var lo = stackLow[top];
    var hi = stackHigh[top];

    if (lo >= hi) continue;

    var pivot = arr[hi];
    var i = lo - 1;

    foreach (var j in lo..<hi)
    {
        if (arr[j] <= pivot)
        {
            i++;
            var swap = arr[i];
            arr[i] = arr[j];
            arr[j] = swap;
        }
    }

    i++;
    var tmp = arr[i];
    arr[i] = arr[hi];
    arr[hi] = tmp;
    var pivotIdx = i;

    if (pivotIdx + 1 < hi)
    {
        stackLow[top] = pivotIdx + 1;
        stackHigh[top] = hi;
        top++;
    }

    if (lo < pivotIdx - 1)
    {
        stackLow[top] = lo;
        stackHigh[top] = pivotIdx - 1;
        top++;
    }
}

var isSorted = true;
foreach (var i in 0..<(n - 1))
{
    if (arr[i] > arr[i + 1]) { isSorted = false; break; }
}

var result = isSorted ? "sorted:" : "unsorted:";
foreach (var i in 0..<n)
{
    if (i > 0) result += ",";
    result += arr[i].ToString();
}

return result;
