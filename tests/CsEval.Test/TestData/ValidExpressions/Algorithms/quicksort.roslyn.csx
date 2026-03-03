{
    // Iterative quicksort using explicit stack simulation
    // Exercises: arrays, nested loops, swaps, conditionals, while loops, break
    var arr = new[] { 38, 27, 43, 3, 9, 82, 10, 64, 15, 51, 29, 77, 1, 99, 44 };
    var n = arr.Length;

    // Simulate stack with pre-sized arrays (max depth = n)
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

        // Partition using last element as pivot
        var pivot = arr[hi];
        var i = lo - 1;

        for (var j = lo; j < hi; j++)
        {
            if (arr[j] <= pivot)
            {
                i++;
                var swap = arr[i];
                arr[i] = arr[j];
                arr[j] = swap;
            }
        }

        // Place pivot in correct position
        i++;
        var tmp = arr[i];
        arr[i] = arr[hi];
        arr[hi] = tmp;
        var pivotIdx = i;

        // Push right subarray
        if (pivotIdx + 1 < hi)
        {
            stackLow[top] = pivotIdx + 1;
            stackHigh[top] = hi;
            top++;
        }

        // Push left subarray
        if (lo < pivotIdx - 1)
        {
            stackLow[top] = lo;
            stackHigh[top] = pivotIdx - 1;
            top++;
        }
    }

    // Verify sorted and build result
    var isSorted = true;
    for (var i = 0; i < n - 1; i++)
    {
        if (arr[i] > arr[i + 1]) { isSorted = false; break; }
    }

    var result = isSorted ? "sorted:" : "unsorted:";
    for (var i = 0; i < n; i++)
    {
        if (i > 0) result += ",";
        result += arr[i].ToString();
    }

    return result;
}