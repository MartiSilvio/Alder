// Finally_TryFinallyWithoutCatch_FinallyRuns
var x = 0;
try {
    x = 5;
} finally {
    x = x + 10;
}
return x;
