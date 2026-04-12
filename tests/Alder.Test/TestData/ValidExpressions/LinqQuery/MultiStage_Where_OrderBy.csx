// §12.20: multi-stage from/where/orderby/select pipeline
var list = new[] { 5, 1, 4, 2, 3 };
return (
    from x in list
    where x % 2 == 1
    orderby x
    select x * 10
).First();
