// §12.8.16.7: anonymous properties participate in arithmetic expressions
var item = new { Price = 10, Qty = 3 };
return item.Price * item.Qty;
