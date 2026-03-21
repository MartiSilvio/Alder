{
    // Extreme algorithm: 2-SAT satisfiability via implication graph + Kosaraju SCC.
    // Advanced syntax: switch expressions, deconstruction, catch when, lock, ??=

    var vars = 14;
    var clauses = new[] {
        1,  2,   -1, 3,   -2, 4,    -3, -4,   5, -1,    -5, 6,    -6, 7,
        -7, 8,    2, -8,   9, 10,   -9, -10,  11, -2,   -11, 12,  -12, 13,
        -13, 14,  -14, 1,  3, -9,    4, -10,  -5, -11,  6, -12,   7, -13,
        8, -14,   -2, -3,  -4, 5,   -6, 9,    10, -11,  -7, -8
    };

    var m = clauses.Length / 2;

    try
    {
        if (vars <= 0 || m == 0) throw new ArgumentException("invalid-instance");
    }
    catch (ArgumentException ex) when (ex.Message.Contains("invalid"))
    {
        return "invalid-2sat";
    }

    var n = vars * 2; // node per literal
    var g = new int[n * n];
    var gr = new int[n * n];
    for (var i = 0; i < g.Length; i++) { g[i] = 0; gr[i] = 0; }

    // map literal to node:
    // +x => 2*(x-1), -x => 2*(x-1)+1
    for (var i = 0; i < m; i++)
    {
        var a = clauses[i * 2];
        var b = clauses[i * 2 + 1];

        var av = Math.Abs(a) - 1;
        var bv = Math.Abs(b) - 1;
        var an = (a > 0 ? 0 : 1);
        var bn = (b > 0 ? 0 : 1);

        var A = av * 2 + an;
        var B = bv * 2 + bn;
        var notA = av * 2 + (1 - an);
        var notB = bv * 2 + (1 - bn);

        // (!a -> b) and (!b -> a)
        g[notA * n + B] = 1;
        g[notB * n + A] = 1;
        gr[B * n + notA] = 1;
        gr[A * n + notB] = 1;
    }

    // First pass order (iterative DFS)
    var vis = new bool[n];
    for (var i = 0; i < n; i++) vis[i] = false;
    var order = new int[n];
    var orderCount = 0;

    var stNode = new int[n * 2];
    var stNext = new int[n * 2];

    for (var s = 0; s < n; s++)
    {
        if (vis[s]) continue;

        var sp = 0;
        stNode[sp] = s;
        stNext[sp] = 0;
        vis[s] = true;
        sp++;

        while (sp > 0)
        {
            var top = sp - 1;
            var u = stNode[top];
            var nxt = stNext[top];
            var advanced = false;

            for (var v = nxt; v < n; v++)
            {
                stNext[top] = v + 1;
                if (g[u * n + v] != 0 && !vis[v])
                {
                    vis[v] = true;
                    stNode[sp] = v;
                    stNext[sp] = 0;
                    sp++;
                    advanced = true;
                    break;
                }
            }

            if (!advanced)
            {
                order[orderCount++] = u;
                sp--;
            }
        }
    }

    // Second pass comps on reversed graph
    var comp = new int[n];
    for (var i = 0; i < n; i++) comp[i] = -1;
    var compCount = 0;

    for (var idx = orderCount - 1; idx >= 0; idx--)
    {
        var start = order[idx];
        if (comp[start] != -1) continue;

        var q = new int[n];
        var head = 0;
        var tail = 0;
        q[tail++] = start;
        comp[start] = compCount;

        while (head < tail)
        {
            var u = q[head++];
            for (var v = 0; v < n; v++)
            {
                if (gr[u * n + v] != 0 && comp[v] == -1)
                {
                    comp[v] = compCount;
                    q[tail++] = v;
                }
            }
        }

        compCount++;
    }

    // Satisfiable iff x and !x not in same component
    var sat = true;
    for (var x = 0; x < vars; x++)
    {
        var pos = x * 2;
        var neg = x * 2 + 1;
        if (comp[pos] == comp[neg]) { sat = false; break; }
    }

    // Assignment: higher postorder component wins.
    var assign = new bool[vars];
    for (var x = 0; x < vars; x++)
    {
        var pos = x * 2;
        var neg = x * 2 + 1;
        assign[x] = comp[pos] > comp[neg];
    }

    // Verify all clauses under assignment
    var satisfied = 0;
    for (var i = 0; i < m; i++)
    {
        var a = clauses[i * 2];
        var b = clauses[i * 2 + 1];

        var av = Math.Abs(a) - 1;
        var bv = Math.Abs(b) - 1;

        var va = a > 0 ? assign[av] : !assign[av];
        var vb = b > 0 ? assign[bv] : !assign[bv];
        if (va || vb) satisfied++;
    }

    var density = (satisfied * 100 / m) switch
    {
        100 => "full",
        >= 90 => "high",
        >= 75 => "mid",
        _ => "low"
    };

    object lockObj = new object();
    string? profile = null;
    lock (lockObj)
    {
        var (s, c) = (satisfied, compCount);
        profile ??= $"satClauses={s},comps={c}";
    }

    var bits = "";
    var hash = 41;
    for (var i = 0; i < vars; i++)
    {
        bits += assign[i] ? "1" : "0";
        hash = hash * 37 + (assign[i] ? (i + 11) : (i + 3));
    }

    return $"sat={sat}|{profile}|density={density}|hash={hash}|assign={bits}";
}
