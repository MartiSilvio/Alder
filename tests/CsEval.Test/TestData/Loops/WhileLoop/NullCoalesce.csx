int? maybeNull = null;
var count = 0;
var num = maybeNull ?? 5;
while (num > 0) {
    count = count + 1;
    num = num - 1;
}
return count;
