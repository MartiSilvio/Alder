{
    // Compute first 20 Fibonacci numbers using an array as memoization cache
    var memo = new[] { 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L,
                       0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L };
    var count = memo.Length;
    memo[0] = 0L;
    memo[1] = 1L;

    // Build up Fibonacci sequence iteratively
    for (var i = 2; i < count; i++)
    {
        memo[i] = memo[i - 1] + memo[i - 2];
    }

    // Verify some known Fibonacci values
    var checksOk = true;
    if (memo[0] != 0L) checksOk = false;
    if (memo[1] != 1L) checksOk = false;
    if (memo[6] != 8L) checksOk = false;
    if (memo[10] != 55L) checksOk = false;
    if (memo[19] != 4181L) checksOk = false;

    // Calculate sum of all Fibonacci numbers in the sequence
    var sum = 0L;
    for (var i = 0; i < count; i++)
    {
        sum = sum + memo[i];
    }

    // Build result string with the full sequence
    var result = "";
    for (var i = 0; i < count; i++)
    {
        if (i > 0)
            result = result + ",";
        result = result + memo[i].ToString();
    }

    return (checksOk ? "ok:" : "fail:") + sum + ":" + result;
}
