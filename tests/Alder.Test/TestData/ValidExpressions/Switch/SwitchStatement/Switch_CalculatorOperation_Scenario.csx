var a = 10.0;
var b = 3.0;
var op = "/";
var calcResult = 0.0;
switch (op) {
    case "+":
        calcResult = a + b;
        break;
    case "-":
        calcResult = a - b;
        break;
    case "*":
        calcResult = a * b;
        break;
    case "/":
        calcResult = a / b;
        break;
    default:
        calcResult = 0;
        break;
}
return calcResult;
