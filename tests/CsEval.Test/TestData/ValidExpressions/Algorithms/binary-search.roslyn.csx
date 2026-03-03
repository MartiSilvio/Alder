{
    // Binary search with multiple variations
    var sorted = new[] { 2, 5, 8, 12, 16, 23, 38, 42, 56, 72, 91, 105 };
    var result = "";

    // Standard binary search
    var target = 23;
    var low = 0;
    var high = sorted.Length - 1;
    var foundIndex = -1;

    while (low <= high)
    {
        var mid = low + (high - low) / 2;
        if (sorted[mid] == target)
        {
            foundIndex = mid;
            break;
        }
        else if (sorted[mid] < target)
            low = mid + 1;
        else
            high = mid - 1;
    }
    result += $"found23={foundIndex}|";

    // Search for missing element
    target = 50;
    low = 0;
    high = sorted.Length - 1;
    foundIndex = -1;
    while (low <= high)
    {
        var mid = low + (high - low) / 2;
        if (sorted[mid] == target)
        {
            foundIndex = mid;
            break;
        }
        else if (sorted[mid] < target)
            low = mid + 1;
        else
            high = mid - 1;
    }
    result += $"found50={foundIndex}|";

    // Find insertion point (lower bound)
    target = 20;
    low = 0;
    high = sorted.Length;
    while (low < high)
    {
        var mid = low + (high - low) / 2;
        if (sorted[mid] < target)
            low = mid + 1;
        else
            high = mid;
    }
    result += $"insertionPoint20={low}|";

    // Count elements in range [10, 50]
    var rangeLow = 10;
    var rangeHigh = 50;

    // Find first >= rangeLow
    low = 0;
    high = sorted.Length;
    while (low < high)
    {
        var mid = low + (high - low) / 2;
        if (sorted[mid] < rangeLow)
            low = mid + 1;
        else
            high = mid;
    }
    var rangeStart = low;

    // Find first > rangeHigh
    low = 0;
    high = sorted.Length;
    while (low < high)
    {
        var mid = low + (high - low) / 2;
        if (sorted[mid] <= rangeHigh)
            low = mid + 1;
        else
            high = mid;
    }
    var rangeEnd = low;

    result += $"countInRange={rangeEnd - rangeStart}|";

    // Verify with linear search
    var linearCount = 0;
    for (var i = 0; i < sorted.Length; i++)
    {
        if (sorted[i] >= rangeLow && sorted[i] <= rangeHigh)
            linearCount++;
    }
    result += $"verified={linearCount == (rangeEnd - rangeStart)}|";

    // Search all elements to verify
    var allFound = true;
    for (var i = 0; i < sorted.Length; i++)
    {
        target = sorted[i];
        low = 0;
        high = sorted.Length - 1;
        var found = false;
        while (low <= high)
        {
            var mid = low + (high - low) / 2;
            if (sorted[mid] == target) { found = true; break; }
            else if (sorted[mid] < target) low = mid + 1;
            else high = mid - 1;
        }
        if (!found) { allFound = false; break; }
    }
    result += $"allFound={allFound}";

    return result;
}