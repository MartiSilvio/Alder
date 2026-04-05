var increments = 0;
var body = 0;
for (var i = 0; i < 5; i++)
{
    increments++;
    if (i % 2 == 0) continue;
    body += i;
}
return increments * 100 + body;
