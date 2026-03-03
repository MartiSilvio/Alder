{
    var sorted = [2, 5, 8, 12, 16, 23, 38, 42, 56, 72, 91, 105];
    var result = "";

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

    var rangeLow = 10;
    var rangeHigh = 50;

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

    var linearCount = 0;
    foreach (var i in 0..<sorted.Length)
    {
        if (sorted[i] between rangeLow and rangeHigh)
            linearCount++;
    }
    result += $"verified={linearCount == (rangeEnd - rangeStart)}|";

    var allFound = true;
    foreach (var i in 0..<sorted.Length)
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