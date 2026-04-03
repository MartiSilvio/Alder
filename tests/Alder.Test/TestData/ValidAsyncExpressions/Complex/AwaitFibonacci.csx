var a = 0;
var b = 1;
for (var i = 0; i < 10; i++)
{
    var temp = await Task.FromResult(a + b);
    a = b;
    b = temp;
}
return a;
