var products = new[] { "Apple", "Banana", "Avocado", "Blueberry", "Cherry" };
var categories = new[] { "A", "B", "C" };
var result = from p in products
             join c in categories on p.Substring(0, 1) equals c
             group p by c into g
             orderby g.Key
             select g.Key + ":" + g.Count();
var s = "";
foreach (var item in result) s += (string)item + ";";
return s;
