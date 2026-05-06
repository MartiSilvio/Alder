// Complex_TryCatchFinally_AllParts
var r = "";
try {
    r = "try";
    throw new Exception();
} catch (Exception) {
    r = r + "-catch";
} finally {
    r = r + "-finally";
}
return r;
