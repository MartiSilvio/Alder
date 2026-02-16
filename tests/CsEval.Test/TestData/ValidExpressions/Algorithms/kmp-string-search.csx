{
    // Knuth-Morris-Pratt substring search
    // Exercises: prefix table construction, linear scan with fallback transitions

    var text = "ABABDABACDABABCABABXABABCABABABABCABAB";
    var pattern = "ABABCABAB";

    var n = text.Length;
    var m = pattern.Length;
    var lps = new int[m];

    // Build LPS (longest proper prefix also suffix) table.
    lps[0] = 0;
    var len = 0;
    var i = 1;
    while (i < m)
    {
        if (pattern[i] == pattern[len])
        {
            len++;
            lps[i] = len;
            i++;
        }
        else if (len != 0)
        {
            len = lps[len - 1];
        }
        else
        {
            lps[i] = 0;
            i++;
        }
    }

    // KMP search for all occurrences.
    var positions = new int[16];
    for (var p = 0; p < positions.Length; p++) positions[p] = -1;
    var found = 0;

    var ti = 0;
    var pj = 0;
    while (ti < n)
    {
        if (text[ti] == pattern[pj])
        {
            ti++;
            pj++;
        }

        if (pj == m)
        {
            positions[found] = ti - pj;
            found++;
            pj = lps[pj - 1];
        }
        else if (ti < n && text[ti] != pattern[pj])
        {
            if (pj != 0) pj = lps[pj - 1];
            else ti++;
        }
    }

    // Additional validation by naive check for the first hit.
    var first = positions[0];
    var naiveOk = true;
    if (first >= 0)
    {
        for (var k = 0; k < m; k++)
        {
            if (text[first + k] != pattern[k]) { naiveOk = false; break; }
        }
    }

    var lpsStr = "";
    for (var k = 0; k < m; k++)
    {
        lpsStr += lps[k].ToString();
        if (k < m - 1) lpsStr += ",";
    }

    var posStr = "";
    for (var k = 0; k < found; k++)
    {
        posStr += positions[k].ToString();
        if (k < found - 1) posStr += ",";
    }

    return $"matches={found}|first={first}|naiveOk={naiveOk}|lps={lpsStr}|positions={posStr}";
}
