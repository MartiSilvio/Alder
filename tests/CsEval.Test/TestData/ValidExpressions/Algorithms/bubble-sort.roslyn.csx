{
    // Bubble sort: sort an array of integers using nested loops and swaps
    var numbers = new[] { 64, 34, 25, 12, 22, 11, 90, 1, 45, 78 };
    var n = numbers.Length;

    // Bubble sort algorithm with early termination
    for (var i = 0; i < n - 1; i++)
    {
        var swapped = false;
        for (var j = 0; j < n - i - 1; j++)
        {
            if (numbers[j] > numbers[j + 1])
            {
                // Swap using a temp variable
                var temp = numbers[j];
                numbers[j] = numbers[j + 1];
                numbers[j + 1] = temp;
                swapped = true;
            }
        }
        // If no swaps occurred in this pass, array is sorted
        if (!swapped)
            break;
    }

    // Verify the array is sorted by checking each adjacent pair
    var isSorted = true;
    for (var i = 0; i < numbers.Length - 1; i++)
    {
        if (numbers[i] > numbers[i + 1])
        {
            isSorted = false;
            break;
        }
    }

    // Build result string with sorted values
    var result = "";
    for (var i = 0; i < numbers.Length; i++)
    {
        if (i > 0)
            result = result + ",";
        result = result + numbers[i].ToString();
    }

    return (isSorted ? "sorted:" : "unsorted:") + result;
}