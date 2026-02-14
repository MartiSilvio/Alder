{
    // sizeof for built-in value types (Plan 09: GAP-09)
    var total = sizeof(int) + sizeof(double) + sizeof(char) + sizeof(bool) + sizeof(byte) + sizeof(long) + sizeof(decimal);
    return total;
}
