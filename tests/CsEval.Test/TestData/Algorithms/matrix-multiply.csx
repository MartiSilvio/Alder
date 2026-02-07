{
    // Multiply two 3x3 matrices using flat arrays (row-major layout)
    // Matrix A: [[1,2,3],[4,5,6],[7,8,9]]
    var a = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
    // Matrix B: [[9,8,7],[6,5,4],[3,2,1]]
    var b = new[] { 9, 8, 7, 6, 5, 4, 3, 2, 1 };

    var size = 3;
    var result = new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

    // Triple-nested loop for matrix multiplication
    for (var i = 0; i < size; i++)
    {
        for (var j = 0; j < size; j++)
        {
            var sum = 0;
            for (var k = 0; k < size; k++)
            {
                sum = sum + a[i * size + k] * b[k * size + j];
            }
            result[i * size + j] = sum;
        }
    }

    // Format result matrix as string: "row;row;row" where each row is "v,v,v"
    var output = "";
    for (var i = 0; i < size; i++)
    {
        if (i > 0)
            output = output + ";";
        for (var j = 0; j < size; j++)
        {
            if (j > 0)
                output = output + ",";
            output = output + result[i * size + j].ToString();
        }
    }

    // Verify trace (sum of diagonal elements)
    var trace = result[0] + result[4] + result[8];
    return "matrix:" + output + ",trace:" + trace;
}
