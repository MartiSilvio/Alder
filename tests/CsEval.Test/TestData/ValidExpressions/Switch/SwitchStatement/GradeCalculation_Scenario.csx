{
    var category = 8;
    var grade = "";
    switch (category) {
        case 10:
        case 9:
            grade = "A";
            break;
        case 8:
            grade = "B";
            break;
        case 7:
            grade = "C";
            break;
        case 6:
            grade = "D";
            break;
        default:
            grade = "F";
            break;
    }
    return grade;
}
