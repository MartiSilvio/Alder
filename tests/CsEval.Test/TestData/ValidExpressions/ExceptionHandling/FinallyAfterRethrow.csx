// Complex_FinallyAfterRethrow
var x = 0;
try {
    try {
        throw new Exception();
    } catch (Exception) {
        x = 1;
        throw;
    } finally {
        x = x + 10;
    }
} catch (Exception) {
    x = x + 100;
}
return x;
