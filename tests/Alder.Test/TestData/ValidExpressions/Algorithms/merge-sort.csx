var arr = [38, 27, 43, 3, 9, 82, 10, 64, 15, 51, 29, 77, 1, 99, 44, 33];
var n = arr.Length;
var temp = new int[n];
var comparisons = 0;

var width = 1;
while (width < n)
{
    var i = 0;
    while (i < n)
    {
        var left = i;
        var mid = i + width;
        if (mid > n) mid = n;
        var right = i + 2 * width;
        if (right > n) right = n;

        var l = left;
        var r = mid;
        var t = left;

        while (l < mid && r < right)
        {
            comparisons++;
            if (arr[l] <= arr[r])
            {
                temp[t] = arr[l];
                l++;
            }
            else
            {
                temp[t] = arr[r];
                r++;
            }
            t++;
        }
        while (l < mid) { temp[t] = arr[l]; l++; t++; }
        while (r < right) { temp[t] = arr[r]; r++; t++; }

        foreach (var c in left..<right)
            arr[c] = temp[c];

        i += 2 * width;
    }
    width *= 2;
}

var isSorted = true;
foreach (var i in 0..<(n - 1))
{
    if (arr[i] > arr[i + 1]) { isSorted = false; break; }
}

var items = "";
foreach (var i in 0..<n)
{
    if (i > 0) items += ",";
    items += arr[i].ToString();
}

return $"sorted={isSorted}|comparisons={comparisons}|first={arr[0]}|last={arr[^1]}|items={items}";
