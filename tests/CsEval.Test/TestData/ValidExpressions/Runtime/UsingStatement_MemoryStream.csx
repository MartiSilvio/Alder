{
    // using statement with IDisposable (Plan 10: GAP-17)
    var result = "";
    using (var ms = new System.IO.MemoryStream())
    {
        result = "len=" + ms.Length.ToString();
    }
    return result;
}
