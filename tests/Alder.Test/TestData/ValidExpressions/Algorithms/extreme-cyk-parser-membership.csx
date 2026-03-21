{
    // Extreme algorithm: CYK parser for CNF grammar membership + parse-count DP.
    // Advanced syntax: switch expressions, deconstruction, catch when, lock, ??=

    // Grammar in CNF-like form over terminals {a,b}
    // Nonterminals: S=0, A=1, B=2, C=3, D=4
    // Binary rules X -> Y Z
    var binary = new[] {
        0,1,2,  // S -> A B
        0,2,3,  // S -> B C
        0,4,1,  // S -> D A
        1,2,4,  // A -> B D
        1,3,2,  // A -> C B
        2,1,1,  // B -> A A
        2,2,2,  // B -> B B
        3,1,3,  // C -> A C
        3,4,2,  // C -> D B
        4,3,4   // D -> C D
    };

    // Terminal rules X -> 'a' or 'b', encode terminal as 0/1
    var terminal = new[] {
        1,0, // A -> a
        2,1, // B -> b
        3,0, // C -> a
        4,1, // D -> b
        0,0  // S -> a (optional)
    };

    var input = "abbaababbab";
    var n = input.Length;
    var nt = 5;

    try
    {
        if (n == 0) throw new ArgumentException("empty-input");
    }
    catch (ArgumentException ex) when (ex.Message.Contains("empty"))
    {
        return "empty";
    }

    // map token char to terminal id without char->int casts
    var alpha = "ab";
    var tok = new int[n];
    for (var i = 0; i < n; i++)
    {
        var idx = alpha.IndexOf(input.Substring(i, 1));
        tok[i] = idx;
    }

    // table[len][start] bitmask of nonterminals producing substring
    // flattened as table[(len*n + start)].
    var table = new int[(n + 1) * n];
    for (var i = 0; i < table.Length; i++) table[i] = 0;

    // parse-count DP for each nonterminal at cell (len,start)
    var ways = new int[(n + 1) * n * nt];
    for (var i = 0; i < ways.Length; i++) ways[i] = 0;

    // length 1 initialization
    for (var i = 0; i < n; i++)
    {
        var mask = 0;
        for (var r = 0; r < terminal.Length / 2; r++)
        {
            var X = terminal[r * 2];
            var t = terminal[r * 2 + 1];
            if (t == tok[i])
            {
                mask |= (1 << X);
                ways[(1 * n + i) * nt + X] += 1;
            }
        }
        table[1 * n + i] = mask;
    }

    // CYK transitions
    for (var len = 2; len <= n; len++)
    {
        for (var start = 0; start + len <= n; start++)
        {
            var cellMask = 0;

            for (var split = 1; split < len; split++)
            {
                var leftMask = table[split * n + start];
                var rightMask = table[(len - split) * n + (start + split)];
                if (leftMask == 0 || rightMask == 0) continue;

                for (var r = 0; r < binary.Length / 3; r++)
                {
                    var X = binary[r * 3];
                    var Y = binary[r * 3 + 1];
                    var Z = binary[r * 3 + 2];

                    if ((leftMask & (1 << Y)) != 0 && (rightMask & (1 << Z)) != 0)
                    {
                        cellMask |= (1 << X);

                        var leftWays = ways[(split * n + start) * nt + Y];
                        var rightWays = ways[((len - split) * n + (start + split)) * nt + Z];
                        var add = leftWays * rightWays;
                        if (add != 0)
                        {
                            var idx = (len * n + start) * nt + X;
                            ways[idx] += add;
                        }
                    }
                }
            }

            table[len * n + start] = cellMask;
        }
    }

    var startMask = table[n * n + 0];
    var accepts = (startMask & (1 << 0)) != 0;
    var parses = ways[(n * n + 0) * nt + 0];

    // symbol-density profile over all cells
    var filled = 0;
    var totalSymbols = 0;
    for (var len = 1; len <= n; len++)
    {
        for (var s = 0; s + len <= n; s++)
        {
            var mask = table[len * n + s];
            if (mask != 0) filled++;
            var bits = 0;
            for (var x = 0; x < nt; x++) if ((mask & (1 << x)) != 0) bits++;
            totalSymbols += bits;
        }
    }

    var density = totalSymbols switch
    {
        > 220 => "dense",
        > 140 => "medium",
        _ => "sparse"
    };

    object lockObj = new object();
    string? meta = null;
    lock (lockObj)
    {
        var (f, ts) = (filled, totalSymbols);
        meta ??= $"cells={f},symbols={ts}";
    }

    var hash = 17;
    for (var len = 1; len <= n; len++)
    {
        for (var s = 0; s + len <= n; s++)
        {
            hash = hash * 31 + table[len * n + s] * (len + 3) + s;
        }
    }

    return $"accepts={accepts}|parses={parses}|{meta}|density={density}|hash={hash}";
}
