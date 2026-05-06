var list = new[] { 5, 3, 1, 4, 2 };
return (from x in list
        orderby x
        select x * 10 into y
        where y > 20
        select y).ToList();
