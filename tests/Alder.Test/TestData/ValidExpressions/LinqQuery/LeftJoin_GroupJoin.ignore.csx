// §12.20: left outer join via group join + DefaultIfEmpty
var a = new[] { 1, 2, 3 };
var b = new[] { 2, 3, 4 };
var result = from x in a
             join y in b on x equals y into grp
             from g in grp.DefaultIfEmpty(-1)
             select x + g;
var total = 0;
foreach (var item in result) total += (int)item;
return total;
