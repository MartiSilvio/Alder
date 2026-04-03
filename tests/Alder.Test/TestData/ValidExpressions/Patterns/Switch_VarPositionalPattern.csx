var pt = (X: 3, Y: 5);
pt switch {
    (0, 0) => "origin",
    (var x, 0) => $"x-axis:{x}",
    (0, var y) => $"y-axis:{y}",
    var (x, y) => $"({x},{y})"
}