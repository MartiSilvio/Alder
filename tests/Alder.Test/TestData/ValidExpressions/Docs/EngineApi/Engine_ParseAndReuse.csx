var items = new[] {
    new { Amount = 150.0, Quantity = 2 },
    new { Amount = 50.0, Quantity = 1 },
    new { Amount = 200.0, Quantity = 3 }
};
var threshold = 100.0;
return items.Where(x => x.Amount > threshold).Sum(x => x.Amount * x.Quantity);
