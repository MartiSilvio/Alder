var a = 0;
var b = 1;
for (var i = 0; i < 10; i++)
{
    var temp = b;
    b = a + b;
    a = temp;
}
return a;
