{
    // Segment tree for range sum with point updates
    // Exercises: tree building, iterative query ranges, updates, index arithmetic

    var data = new[] { 5, 8, 6, 3, 2, 7, 2, 6, 4, 1, 9, 0, 5, 3, 8, 2 };
    var n = data.Length;

    // Build power-of-two base size.
    var size = 1;
    while (size < n) size <<= 1;

    // Tree in flat array: [1..2*size)
    var tree = new int[size * 2];
    for (var i = 0; i < tree.Length; i++) tree[i] = 0;

    // Copy leaves
    for (var i = 0; i < n; i++) tree[size + i] = data[i];
    for (var i = size - 1; i >= 1; i--) tree[i] = tree[i * 2] + tree[i * 2 + 1];

    // Helper: range sum [l, r] inclusive (iterative).
    var q1 = 0;
    {
        var l = 2 + size;
        var r = 9 + size;
        while (l <= r)
        {
            if ((l & 1) == 1) { q1 += tree[l]; l++; }
            if ((r & 1) == 0) { q1 += tree[r]; r--; }
            l >>= 1;
            r >>= 1;
        }
    }

    var q2 = 0;
    {
        var l = 0 + size;
        var r = 15 + size;
        while (l <= r)
        {
            if ((l & 1) == 1) { q2 += tree[l]; l++; }
            if ((r & 1) == 0) { q2 += tree[r]; r--; }
            l >>= 1;
            r >>= 1;
        }
    }

    // Point updates: data[4] = 10, data[10] = 4
    var idx = 4;
    var value = 10;
    {
        var p = size + idx;
        tree[p] = value;
        p >>= 1;
        while (p >= 1)
        {
            tree[p] = tree[p * 2] + tree[p * 2 + 1];
            p >>= 1;
        }
    }

    idx = 10;
    value = 4;
    {
        var p = size + idx;
        tree[p] = value;
        p >>= 1;
        while (p >= 1)
        {
            tree[p] = tree[p * 2] + tree[p * 2 + 1];
            p >>= 1;
        }
    }

    var q3 = 0;
    {
        var l = 2 + size;
        var r = 9 + size;
        while (l <= r)
        {
            if ((l & 1) == 1) { q3 += tree[l]; l++; }
            if ((r & 1) == 0) { q3 += tree[r]; r--; }
            l >>= 1;
            r >>= 1;
        }
    }

    var q4 = 0;
    {
        var l = 8 + size;
        var r = 12 + size;
        while (l <= r)
        {
            if ((l & 1) == 1) { q4 += tree[l]; l++; }
            if ((r & 1) == 0) { q4 += tree[r]; r--; }
            l >>= 1;
            r >>= 1;
        }
    }

    // Validate with direct summation.
    var directTotal = 0;
    for (var i = 0; i < n; i++)
    {
        var v = tree[size + i];
        directTotal += v;
    }

    return $"q1={q1}|q2={q2}|q3={q3}|q4={q4}|root={tree[1]}|direct={directTotal}";
}
