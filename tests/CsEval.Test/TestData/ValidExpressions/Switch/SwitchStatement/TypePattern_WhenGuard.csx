{
    // Switch type patterns with when guards (Plan 08: GAP-23)
    var values = new object[] { -5, 0, 10, "test" };
    var result = "";
    for (var i = 0; i < values.Length; i++)
    {
        switch (values[i])
        {
            case int n when n < 0:
                result += "neg";
                break;
            case int n when n == 0:
                result += "zero";
                break;
            case int n when n > 0:
                result += "pos";
                break;
            case string s:
                result += "str";
                break;
        }
        if (i < values.Length - 1) result += ",";
    }
    return result;
}
