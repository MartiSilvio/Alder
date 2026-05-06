var dict = new Dictionary<string, int>();
dict["x"] = 10;
dict["y"] = 20;
dict["z"] = 30;
string keys = "";
foreach (var key in dict.Keys)
{
    keys += key;
}
return keys.Length;
