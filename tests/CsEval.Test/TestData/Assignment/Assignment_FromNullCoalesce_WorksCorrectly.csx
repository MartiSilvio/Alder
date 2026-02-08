{
    int? maybeNull = null;
    var fallback = 0;
    fallback = maybeNull ?? 42;
    return fallback;
}
