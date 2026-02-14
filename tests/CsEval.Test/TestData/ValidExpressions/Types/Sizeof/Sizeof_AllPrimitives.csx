{
    // sizeof for all primitive value types (Plan 09: GAP-09)
    var result = "";
    result += sizeof(sbyte).ToString();
    result += "," + sizeof(byte).ToString();
    result += "," + sizeof(short).ToString();
    result += "," + sizeof(ushort).ToString();
    result += "," + sizeof(int).ToString();
    result += "," + sizeof(uint).ToString();
    result += "," + sizeof(long).ToString();
    result += "," + sizeof(ulong).ToString();
    result += "," + sizeof(float).ToString();
    result += "," + sizeof(double).ToString();
    result += "," + sizeof(decimal).ToString();
    result += "," + sizeof(char).ToString();
    result += "," + sizeof(bool).ToString();
    return result;
}
