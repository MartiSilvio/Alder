// Complex_ExceptionInFinally_ReplacesOriginal
// In C#, an exception thrown in a finally block replaces the original exception
var r = "";
try {
    try {
        throw new ArgumentException("first");
    } finally {
        throw new InvalidOperationException("second");
    }
} catch (InvalidOperationException ex) {
    r = ex.Message;
}
return r;
