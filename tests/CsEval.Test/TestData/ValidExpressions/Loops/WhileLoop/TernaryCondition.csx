var useLimit = true;
var count = 0;
var i = 0;
while (i < (useLimit ? 5 : 10)) {
    count = count + 1;
    i = i + 1;
}
return count;
