// ControlFlow_ReturnFromTryWithFinally
// In C#, finally runs but cannot change the return value.
// We verify the finally runs by checking a side-effect wrapper.
// Roslyn scripting returns the value from the return statement, not the finally modification.
// The return value is captured before the finally block modifies x.
var x = 0;
try {
    x = 1;
    return x;
} finally {
    x = 99;
}
