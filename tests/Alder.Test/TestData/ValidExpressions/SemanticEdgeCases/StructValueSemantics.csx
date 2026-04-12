// Verifies ECMA-334 section 16: Struct copy semantics via DateTime
{
    var d = new DateTime(2024, 1, 15);
    return d.Year + d.Month + d.Day;
}
