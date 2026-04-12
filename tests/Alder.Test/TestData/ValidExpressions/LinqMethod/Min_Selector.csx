var items = new[]
{
    new { Price = 3.5 },
    new { Price = 1.25 },
    new { Price = 2.75 }
};
return items.Min(i => i.Price);
