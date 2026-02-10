{
    // Finite state machine: tokenizer for arithmetic expressions
    // Exercises: char comparison, string building, state transitions, arrays, nested conditionals
    // States: START=0, NUMBER=1, OPERATOR=2, ERROR=3, DONE=4
    var input = "123+456*78-9";
    var result = "";
    var tokens = "";
    var tokenCount = 0;

    var state = 0;
    var pos = 0;
    var currentToken = "";

    while (pos <= input.Length)
    {
        var ch = pos < input.Length ? input[pos] : '\0';
        var isDigit = ch >= '0' && ch <= '9';
        var isOp = ch == '+' || ch == '-' || ch == '*' || ch == '/';

        if (state == 0) // START
        {
            if (isDigit)
            {
                currentToken = "" + ch;
                state = 1;
                pos++;
            }
            else if (ch == '\0')
            {
                state = 4;
            }
            else
            {
                state = 3;
            }
        }
        else if (state == 1) // NUMBER
        {
            if (isDigit)
            {
                currentToken = currentToken + ch;
                pos++;
            }
            else
            {
                // Emit number token
                if (tokens.Length > 0) tokens += ",";
                tokens += "NUM(" + currentToken + ")";
                tokenCount++;
                currentToken = "";

                if (isOp)
                {
                    if (tokens.Length > 0) tokens += ",";
                    tokens += "OP(" + ch + ")";
                    tokenCount++;
                    pos++;
                    state = 0;
                }
                else if (ch == '\0')
                {
                    state = 4;
                }
                else
                {
                    state = 3;
                }
            }
        }
        else
        {
            break;
        }
    }

    var hasError = state == 3;
    result += $"tokens={tokens}|";
    result += $"count={tokenCount}|";
    result += $"error={hasError}|";

    // Evaluate the tokenized expression left-to-right
    var nums = new int[10];
    var ops = new char[10];
    for (var ci = 0; ci < 10; ci++) ops[ci] = ' ';
    var numIdx = 0;
    var opIdx = 0;
    var curNum = 0;
    var inNum = false;

    for (var i = 0; i <= input.Length; i++)
    {
        var c = i < input.Length ? input[i] : '\0';
        if (c >= '0' && c <= '9')
        {
            curNum = curNum * 10 + (c - '0');
            inNum = true;
        }
        else
        {
            if (inNum)
            {
                nums[numIdx] = curNum;
                numIdx++;
                curNum = 0;
                inNum = false;
            }
            if (c == '+' || c == '-' || c == '*' || c == '/')
            {
                ops[opIdx] = c;
                opIdx++;
            }
        }
    }

    // Evaluate left to right (no precedence)
    var evalResult = nums[0];
    for (var i = 0; i < opIdx; i++)
    {
        if (ops[i] == '+') evalResult = evalResult + nums[i + 1];
        else if (ops[i] == '-') evalResult = evalResult - nums[i + 1];
        else if (ops[i] == '*') evalResult = evalResult * nums[i + 1];
        else if (ops[i] == '/') evalResult = evalResult / nums[i + 1];
    }
    result += $"evalResult={evalResult}";

    return result;
}
