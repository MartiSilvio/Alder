var arr = new[] { 1, 2, 3, 4, 5 };
var sum = 0;
var i = 0;
do {
    sum = sum + arr[i];
    i = i + 1;
} while (i < 5);
return sum;
