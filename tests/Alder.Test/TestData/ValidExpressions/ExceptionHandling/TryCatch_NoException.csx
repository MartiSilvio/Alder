// Complex_TryCatch_NoExceptionThrown_CatchSkipped
var x = 0;
try {
    x = 5;
} catch (Exception) {
    x = -1;
}
return x;
