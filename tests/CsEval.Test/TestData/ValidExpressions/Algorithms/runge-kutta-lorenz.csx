// 4th-order Runge-Kutta integration of the Lorenz attractor
var sigma = 10.0;
var rho = 28.0;
var beta = 8.0 / 3.0;

var lorenzX = (double x, double y, double z) => sigma * (y - x);
var lorenzY = (double x, double y, double z) => x * (rho - z) - y;
var lorenzZ = (double x, double y, double z) => x * y - beta * z;

// RK4 step
var rk4Step = (double x, double y, double z, double h) => {
    var k1x = lorenzX(x, y, z);
    var k1y = lorenzY(x, y, z);
    var k1z = lorenzZ(x, y, z);

    var k2x = lorenzX(x + h / 2 * k1x, y + h / 2 * k1y, z + h / 2 * k1z);
    var k2y = lorenzY(x + h / 2 * k1x, y + h / 2 * k1y, z + h / 2 * k1z);
    var k2z = lorenzZ(x + h / 2 * k1x, y + h / 2 * k1y, z + h / 2 * k1z);

    var k3x = lorenzX(x + h / 2 * k2x, y + h / 2 * k2y, z + h / 2 * k2z);
    var k3y = lorenzY(x + h / 2 * k2x, y + h / 2 * k2y, z + h / 2 * k2z);
    var k3z = lorenzZ(x + h / 2 * k2x, y + h / 2 * k2y, z + h / 2 * k2z);

    var k4x = lorenzX(x + h * k3x, y + h * k3y, z + h * k3z);
    var k4y = lorenzY(x + h * k3x, y + h * k3y, z + h * k3z);
    var k4z = lorenzZ(x + h * k3x, y + h * k3y, z + h * k3z);

    return new double[] {
        x + h * (k1x + 2k2x + 2k3x + k4x) / 6,
        y + h * (k1y + 2k2y + 2k3y + k4y) / 6,
        z + h * (k1z + 2k2z + 2k3z + k4z) / 6
    };
};

var h = 0.001;
var steps = 10000;
var x = 1.0;
var y = 1.0;
var z = 1.0;

var minX = x; var maxX = x;
var minY = y; var maxY = y;
var minZ = z; var maxZ = z;
var crossings = 0;
var prevX = x;

foreach (var i in 0..<steps)
{
    var state = rk4Step(x, y, z, h);
    x = state[0];
    y = state[1];
    z = state[2];

    if (x < minX) minX = x;
    if (x > maxX) maxX = x;
    if (y < minY) minY = y;
    if (y > maxY) maxY = y;
    if (z < minZ) minZ = z;
    if (z > maxZ) maxZ = z;

    if ((prevX > 0 and x <= 0) or (prevX < 0 and x >= 0))
        crossings++;
    prevX = x;
}

var bounded = -25 < minX and maxX < 25
    and -30 < minY and maxY < 30
    and 0 < minZ and maxZ < 55;

var oscillatory = crossings > 50;

var dist = sqrt(x ** 2 + y ** 2 + z ** 2);
var notCollapsed = dist > 1.0;
var notBlownUp = dist < 60.0;

var zPositive = minZ > 0;

var result = $"bounded={bounded}|oscillatory={oscillatory}|crossings={crossings}|";
result += $"notCollapsed={notCollapsed}|notBlownUp={notBlownUp}|zPositive={zPositive}|";
result += $"xRange={Math.Round(maxX - minX, 1)}|zRange={Math.Round(maxZ - minZ, 1)}";

return result;
