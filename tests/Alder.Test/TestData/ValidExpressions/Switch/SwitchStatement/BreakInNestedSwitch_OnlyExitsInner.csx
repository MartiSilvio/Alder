var outer = 1;
var inner = 1;
var log = "";
switch (outer) {
    case 1:
        log = log + "outer1-";
        switch (inner) {
            case 1:
                log = log + "inner1";
                break;
        }
        log = log + "-afterinner";
        break;
}
return log;
