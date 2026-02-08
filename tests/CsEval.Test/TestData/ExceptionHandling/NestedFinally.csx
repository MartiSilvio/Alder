// Nested_BothFinallyBlocksExecute
var x = 0;
try {
    try {
        x = 1;
    } finally {
        x = x + 10;
    }
} finally {
    x = x + 100;
}
return x;
