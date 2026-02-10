{
    int? x = null;
    if (true) {
        x ??= 100;
    }
    return x;
}
