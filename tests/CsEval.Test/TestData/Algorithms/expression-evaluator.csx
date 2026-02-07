{
    // Shunting-yard algorithm: evaluate infix arithmetic with operator precedence
    // Exercises: arrays as stacks, char classification, precedence handling, multi-pass processing
    var input = "3+4*2-1+10/5";

    // Tokenize: extract numbers and operators
    var numStack = new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    var opStack = new[] { ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ' };
    var numTop = 0;
    var opTop = 0;

    var pos = 0;
    while (pos < input.Length)
    {
        var ch = input[pos];

        if (ch >= '0' && ch <= '9')
        {
            // Parse multi-digit number
            var num = 0;
            while (pos < input.Length && input[pos] >= '0' && input[pos] <= '9')
            {
                num = num * 10 + (input[pos] - '0');
                pos++;
            }
            numStack[numTop] = num;
            numTop++;
        }
        else if (ch == '+' || ch == '-' || ch == '*' || ch == '/')
        {
            // Get precedence of current operator
            var currPrec = (ch == '*' || ch == '/') ? 2 : 1;

            // Apply higher-or-equal precedence operators on the stack
            while (opTop > 0)
            {
                var topOp = opStack[opTop - 1];
                var topPrec = (topOp == '*' || topOp == '/') ? 2 : 1;

                if (topPrec >= currPrec)
                {
                    opTop--;
                    var b = numStack[numTop - 1]; numTop--;
                    var a = numStack[numTop - 1]; numTop--;
                    var res = 0;
                    if (topOp == '+') res = a + b;
                    else if (topOp == '-') res = a - b;
                    else if (topOp == '*') res = a * b;
                    else if (topOp == '/') res = a / b;
                    numStack[numTop] = res;
                    numTop++;
                }
                else
                {
                    break;
                }
            }

            opStack[opTop] = ch;
            opTop++;
            pos++;
        }
        else
        {
            pos++;
        }
    }

    // Apply remaining operators
    while (opTop > 0)
    {
        opTop--;
        var op = opStack[opTop];
        var b = numStack[numTop - 1]; numTop--;
        var a = numStack[numTop - 1]; numTop--;
        var res = 0;
        if (op == '+') res = a + b;
        else if (op == '-') res = a - b;
        else if (op == '*') res = a * b;
        else if (op == '/') res = a / b;
        numStack[numTop] = res;
        numTop++;
    }

    var finalResult = numStack[0];

    // Verify: 3+4*2-1+10/5 = 3+8-1+2 = 12
    var result = $"expr={input}|result={finalResult}|correct={finalResult == 12}";

    return result;
}
