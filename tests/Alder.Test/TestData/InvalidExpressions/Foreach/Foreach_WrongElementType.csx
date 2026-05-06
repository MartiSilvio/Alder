// §13.9.5: element type mismatch — cannot iterate int[] with string loop variable
int[] arr = new int[] { 1, 2, 3 };
string result = "";
foreach (string s in arr)
    result += s;
return result;
