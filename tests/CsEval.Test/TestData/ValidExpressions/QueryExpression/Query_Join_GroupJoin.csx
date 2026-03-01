{
    var categories = new[] { "A", "B", "C" };
    var items = new[] { "A1", "A2", "B1" };
    var result = from c in categories
                 join i in items on c equals i.Substring(0, 1) into grp
                 select grp.Count();
    var sum = 0;
    foreach (var item in result) sum += (int)item;
    return sum;
}
