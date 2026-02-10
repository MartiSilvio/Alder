// Complex_DeepNesting_ThreeLevels
var r = "";
try {
    try {
        try { throw new ArgumentException("deep"); }
        catch (InvalidOperationException) { r = "level1"; }
    }
    catch (FormatException) { r = "level2"; }
}
catch (ArgumentException ex) { r = ex.Message; }
return r;
