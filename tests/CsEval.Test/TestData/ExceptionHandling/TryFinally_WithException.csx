// Finally_TryFinallyWithException_FinallyRunsBeforePropagation
// The finally should still run even though the exception propagates.
// We wrap in an outer try/catch to observe both the finally side-effect and the exception.
var x = 0;
try {
    try {
        throw new Exception("inner");
    } finally {
        x = 99;
    }
} catch (Exception) { }
return x;
