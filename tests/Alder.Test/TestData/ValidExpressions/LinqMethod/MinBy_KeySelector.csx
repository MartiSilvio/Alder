var items = new[]
{
    new { Name = "a", Price = 3.5 },
    new { Name = "b", Price = 1.25 },
    new { Name = "c", Price = 2.75 }
};
return items.MinBy(i => i.Price).Name;
