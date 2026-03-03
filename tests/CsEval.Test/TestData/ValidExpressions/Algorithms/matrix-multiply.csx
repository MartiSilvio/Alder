{
    var a = [1, 2, 3, 4, 5, 6, 7, 8, 9];
    var b = [9, 8, 7, 6, 5, 4, 3, 2, 1];

    var size = 3;
    var result = new int[9];

    foreach (var i in 0..<size)
    {
        foreach (var j in 0..<size)
        {
            var sum = 0;
            foreach (var k in 0..<size)
            {
                sum = sum + a[i * size + k] * b[k * size + j];
            }
            result[i * size + j] = sum;
        }
    }

    var output = "";
    foreach (var i in 0..<size)
    {
        if (i > 0)
            output = output + ";";
        foreach (var j in 0..<size)
        {
            if (j > 0)
                output = output + ",";
            output = output + result[i * size + j].ToString();
        }
    }

    var trace = result[0] + result[4] + result[8];
    return "matrix:" + output + ",trace:" + trace;
}
