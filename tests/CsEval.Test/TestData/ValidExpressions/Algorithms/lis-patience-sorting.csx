// Longest Increasing Subsequence with patience sorting technique (O(n log n))
// Exercises: binary search over tails, predecessor tracking, reconstruction

var a = new[] { 10, 9, 2, 5, 3, 7, 101, 18, 19, 20, 1, 4, 6, 8 };
var n = a.Length;

var tailsVal = new int[n];
var tailsIdx = new int[n];
var prev = new int[n];
for (var i = 0; i < n; i++) prev[i] = -1;

var len = 0;
for (var i = 0; i < n; i++)
{
    // lower_bound on tailsVal[0..len)
    var lo = 0;
    var hi = len;
    while (lo < hi)
    {
        var mid = (lo + hi) / 2;
        if (tailsVal[mid] < a[i]) lo = mid + 1;
        else hi = mid;
    }
    var pos = lo;

    tailsVal[pos] = a[i];
    tailsIdx[pos] = i;
    if (pos > 0) prev[i] = tailsIdx[pos - 1];

    if (pos == len) len++;
}

// Reconstruct one LIS.
var seq = new int[len];
var idx = tailsIdx[len - 1];
for (var k = len - 1; k >= 0; k--)
{
    seq[k] = a[idx];
    idx = prev[idx];
}

// Validate strictly increasing.
var increasing = true;
for (var i = 1; i < len; i++)
{
    if (seq[i] <= seq[i - 1]) { increasing = false; break; }
}

var s = "";
for (var i = 0; i < len; i++)
{
    s += seq[i].ToString();
    if (i < len - 1) s += ",";
}

return $"len={len}|increasing={increasing}|seq={s}|tailLast={tailsVal[len - 1]}";
