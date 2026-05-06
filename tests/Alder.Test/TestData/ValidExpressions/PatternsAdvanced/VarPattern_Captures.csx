// §11.2.4: var pattern always matches and captures
object o = 7;
if (o is var x)
    return x.ToString();
return "no";
