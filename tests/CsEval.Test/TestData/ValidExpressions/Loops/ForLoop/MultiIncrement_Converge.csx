{
    // Converging loop with multi-increment (Plan 08: GAP-12, GAP-13)
    var steps = 0;
    for (int lo = 0, hi = 100; lo < hi; lo += 3, hi -= 2)
    {
        steps++;
    }
    return steps;
}
