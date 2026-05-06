var result = [];
var i = 0;
do {
    result = [..result, i * 2];
    i = i + 1;
} while (i < 5);
return result;
