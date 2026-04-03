var list = new List<int> { 1, 2, 3, 4, 5 };
var product = 1;
foreach (var n in list)
{
    product *= await Task.FromResult(n);
}
return product;
