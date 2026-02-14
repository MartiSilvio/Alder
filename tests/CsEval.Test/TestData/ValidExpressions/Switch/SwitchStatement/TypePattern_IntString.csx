{
    // Switch type patterns with variable binding (Plan 08: GAP-23)
    var items = new object[] { 42, "hello", 7, "world" };
    var result = "";
    for (var i = 0; i < items.Length; i++)
    {
        switch (items[i])
        {
            case int n:
                result += "i" + n.ToString();
                break;
            case string s:
                result += "s" + s;
                break;
        }
    }
    return result;
}
