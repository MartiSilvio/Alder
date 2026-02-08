// Complex_CatchWhen_WithMemberAccess
var r = 0;
try {
    throw new ArgumentException("hello world");
} catch (ArgumentException ex) when (ex.Message.Length > 5) {
    r = 1;
} catch (Exception) {
    r = 2;
}
return r;
