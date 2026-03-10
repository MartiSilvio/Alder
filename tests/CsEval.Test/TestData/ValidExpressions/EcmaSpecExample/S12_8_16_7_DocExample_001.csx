{
  var p1 = new { Name = "Lawnmower", Price = 495.00 };
  var p2 = new { Name = "Shovel", Price = 26.95 };
  p1 = p2;
  return p1.Price;
}
