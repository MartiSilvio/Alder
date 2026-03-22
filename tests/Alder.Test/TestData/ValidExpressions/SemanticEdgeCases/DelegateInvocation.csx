// Verifies ECMA-334 section 20: Delegate creation from lambda and invocation
{
    Func<int, int> f = x => x * 2;
    return f(5);
}
