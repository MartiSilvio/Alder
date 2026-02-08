// Complex_CatchWhen_GuardFalse_FallsThroughAll
// When guard fails on all typed catches, the bare catch handles it
var r = 0;
try {
    throw new Exception("x");
} catch (Exception ex) when (ex.Message == "y") {
    r = 1;
} catch {
    r = 2;
}
return r;
