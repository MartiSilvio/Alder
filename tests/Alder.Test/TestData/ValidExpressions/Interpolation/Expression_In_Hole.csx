// §12.8.3: complex expression inside interpolation hole
var items = new[] { 1, 2, 3, 4, 5 };
return $"sum={items.Sum()}, count={items.Length}";
