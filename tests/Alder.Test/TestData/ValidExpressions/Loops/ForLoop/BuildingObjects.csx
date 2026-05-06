object lastObj = null;
for (var i = 0; i < 3; i++) {
    lastObj = new { Index = i, Squared = i * i };
}
return lastObj;
