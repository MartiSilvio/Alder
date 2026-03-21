var arr = [38, 27, 43, 3, 9, 82, 10, 15, 42, 99, 7, 23, 56, 11, 44, 88];
var n = arr.Length;
var temp = new int[n];

for (var width = 1; width < n; width *= 2)
{
    for (var i = 0; i < n; i += 2 * width)
    {
        var left = i;
        var mid = i + width;
        var right = i + 2 * width;
        if (mid > n) mid = n;
        if (right > n) right = n;

        var l = left;
        var r = mid;
        var t = left;
        while (l < mid && r < right)
        {
            if (arr[l] <= arr[r])
                temp[t++] = arr[l++];
            else
                temp[t++] = arr[r++];
        }
        while (l < mid) temp[t++] = arr[l++];
        while (r < right) temp[t++] = arr[r++];

        foreach (var c in left..<right)
            arr[c] = temp[c];
    }
}

var result = "";
foreach (var i in 0..<n)
{
    if (i > 0) result += ",";
    result += arr[i].ToString();
}
result
