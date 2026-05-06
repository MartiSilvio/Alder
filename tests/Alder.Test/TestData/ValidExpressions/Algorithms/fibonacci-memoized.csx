var memo = new long[20];
var count = memo.Length;
memo[0] = 0L;
memo[1] = 1L;

foreach (var i in 2..<count)
{
    memo[i] = memo[i - 1] + memo[i - 2];
}

var checksOk = true;
if (memo[0] != 0L) checksOk = false;
if (memo[1] != 1L) checksOk = false;
if (memo[6] != 8L) checksOk = false;
if (memo[10] != 55L) checksOk = false;
if (memo[^1] != 4181L) checksOk = false;

var sum = 0L;
foreach (var i in 0..<count)
{
    sum = sum + memo[i];
}

var result = "";
foreach (var i in 0..<count)
{
    if (i > 0)
        result = result + ",";
    result = result + memo[i].ToString();
}

return (checksOk ? "ok:" : "fail:") + sum + ":" + result;
