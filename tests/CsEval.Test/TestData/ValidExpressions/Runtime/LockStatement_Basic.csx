{
    // lock statement (Plan 10: GAP-18)
    var lockObj = new object();
    var x = 0;
    lock (lockObj)
    {
        x = 42;
    }
    return x;
}
