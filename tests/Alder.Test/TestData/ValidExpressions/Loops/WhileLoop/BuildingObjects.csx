var i = 0;
object lastObj = null;
while (i < 3) {
    lastObj = new { Index = i, Squared = i * i };
    i = i + 1;
}
return lastObj;
