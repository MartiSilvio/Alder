var numbers = [64, 34, 25, 12, 22, 11, 90, 1, 45, 78];
var n = numbers.Length;

foreach (var i in 0..<(n - 1))
{
    var swapped = false;
    foreach (var j in 0..<(n - i - 1))
    {
        if (numbers[j] > numbers[j + 1])
        {
            var temp = numbers[j];
            numbers[j] = numbers[j + 1];
            numbers[j + 1] = temp;
            swapped = true;
        }
    }
    if (!swapped)
        break;
}

var isSorted = true;
foreach (var i in 0..<(numbers.Length - 1))
{
    if (numbers[i] > numbers[i + 1])
    {
        isSorted = false;
        break;
    }
}

var result = "";
foreach (var i in 0..<numbers.Length)
{
    if (i > 0)
        result = result + ",";
    result = result + numbers[i].ToString();
}

return (isSorted ? "sorted:" : "unsorted:") + result;
