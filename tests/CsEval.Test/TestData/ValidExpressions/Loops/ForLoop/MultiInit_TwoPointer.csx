{
    // Two-pointer reversal using for multi-init and multi-increment (Plan 08: GAP-12, GAP-13)
    var arr = new[] { 1, 2, 3, 4, 5 };
    for (int i = 0, j = arr.Length - 1; i < j; i++, j--)
    {
        var tmp = arr[i];
        arr[i] = arr[j];
        arr[j] = tmp;
    }
    var result = "";
    for (var k = 0; k < arr.Length; k++)
    {
        if (k > 0) result += ",";
        result += arr[k].ToString();
    }
    return result;
}
