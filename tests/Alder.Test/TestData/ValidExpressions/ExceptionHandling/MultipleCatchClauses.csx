// Complex_MultipleCatchClauses_OrderMatters
var r = "";
try {
    throw new ArgumentNullException();
} catch (ArgumentNullException) {
    r = "null";
} catch (ArgumentException) {
    r = "arg";
} catch (Exception) {
    r = "ex";
}
return r;
