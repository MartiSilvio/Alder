{
    var items = [1, 2, 3, 2, 1];
    var countOnes = 0;
    var countTwos = 0;
    var countOthers = 0;
    foreach (var item in items) {
        switch (item) {
            case 1:
                countOnes = countOnes + 1;
                break;
            case 2:
                countTwos = countTwos + 1;
                break;
            default:
                countOthers = countOthers + 1;
                break;
        }
    }
    return countOnes * 100 + countTwos * 10 + countOthers;
}
