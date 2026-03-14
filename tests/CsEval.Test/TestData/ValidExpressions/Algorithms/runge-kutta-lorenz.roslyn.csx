var sigma = 10.0;
var rho = 28.0;
var beta = 8.0 / 3.0;

var lorenzX = (double x, double y, double z) => sigma * (y - x);
var lorenzY = (double x, double y, double z) => x * (rho - z) - y;
var lorenzZ = (double x, double y, double z) => x * y - beta * z;

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
        x + h * (k1x + 2 * k2x + 2 * k3x + k4x) / 6,
        y + h * (k1y + 2 * k2y + 2 * k3y + k4y) / 6,
        z + h * (k1z + 2 * k2z + 2 * k3z + k4z) / 6
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

for (var i = 0; i < steps; i++)
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

    if ((prevX > 0 && x <= 0) || (prevX < 0 && x >= 0))
        crossings++;
    prevX = x;
}

var bounded = -25 < minX && maxX < 25
    && -30 < minY && maxY < 30
    && 0 < minZ && maxZ < 55;

var oscillatory = crossings > 50;

var dist = Math.Sqrt(x * x + y * y + z * z);
var notCollapsed = dist > 1.0;
var notBlownUp = dist < 60.0;

var zPositive = minZ > 0;

var result = $"bounded={bounded}|oscillatory={oscillatory}|crossings={crossings}|";
result += $"notCollapsed={notCollapsed}|notBlownUp={notBlownUp}|zPositive={zPositive}|";
result += $"xRange={Math.Round(maxX - minX, 1)}|zRange={Math.Round(maxZ - minZ, 1)}";

return result;
