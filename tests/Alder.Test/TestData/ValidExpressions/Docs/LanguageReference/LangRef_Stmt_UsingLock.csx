{
    var obj = new object();
    lock (obj)
    {
        return 42;
    }
}
