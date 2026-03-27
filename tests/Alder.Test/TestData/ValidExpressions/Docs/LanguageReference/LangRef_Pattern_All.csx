{
    object x = "hello world";
    var isStr = x is string;
    var isNotNull = x is not null;
    var isLong = x is string { Length: > 5 };
    var binding = x is string s ? s.Length : -1;

    var arr = new[] { 1, 2, 3, 4, 5 };
    var listMatch = arr is [1, .., 5];
    var relational = 75 is > 0 and <= 100;

    var switchResult = x switch
    {
        string str when str.Length > 5 => "long",
        string => "short",
        _ => "other"
    };

    return isStr.ToString() + "|" + isNotNull + "|" + isLong + "|" + binding + "|" + listMatch + "|" + relational + "|" + switchResult;
}
