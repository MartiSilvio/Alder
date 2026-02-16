{
    var result = Array.Empty<int>();
    var i = 0;
    do {
        result = result.Append(i * 2).ToArray();
        i = i + 1;
    } while (i < 5);
    return result;
}
