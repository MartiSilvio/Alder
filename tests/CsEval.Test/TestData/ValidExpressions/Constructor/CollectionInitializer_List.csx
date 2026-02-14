{
    // Collection initializer calls Add method on List<int>
    var list = new System.Collections.Generic.List<int>() { 10, 20, 30 };
    return list[0] + list[1] + list[2];
}
