// Complex_NestedTryFinally_BothRun
var x = "";
try {
    x = x + "a";
    try {
        x = x + "b";
    } finally {
        x = x + "c";
    }
} finally {
    x = x + "d";
}
return x;
