// Rabin-Karp multi-hit substring search with rolling hash
// Exercises: modular arithmetic, rolling window updates, collision check, switch logic

var text = "the_quick_brown_fox_jumps_over_the_lazy_dog_the_quick_brown_fox";
var pattern = "quick";
var n = text.Length;
var m = pattern.Length;

var baseVal = 257;
var mod = 1_000_000_007;

var power = 1;
for (var i = 0; i < m - 1; i++) power = (int)((long)power * baseVal % mod);

var patHash = 0;
var winHash = 0;
for (var i = 0; i < m; i++)
{
    patHash = (int)(((long)patHash * baseVal + (int)pattern[i]) % mod);
    winHash = (int)(((long)winHash * baseVal + (int)text[i]) % mod);
}

var hits = new int[16];
for (var i = 0; i < hits.Length; i++) hits[i] = -1;
var found = 0;
var collisions = 0;

for (var i = 0; i <= n - m; i++)
{
    if (patHash == winHash)
    {
        var equal = true;
        for (var j = 0; j < m; j++)
        {
            if (text[i + j] != pattern[j]) { equal = false; break; }
        }

        switch (equal)
        {
            case true:
                hits[found++] = i;
                break;
            case false:
                collisions++;
                break;
        }
    }

    if (i < n - m)
    {
        var left = (int)text[i];
        var right = (int)text[i + m];

        var removed = (long)left * power % mod;
        var tmp = winHash - (int)removed;
        if (tmp < 0) tmp += mod;
        winHash = (int)(((long)tmp * baseVal + right) % mod);
    }
}

var pos = "";
for (var i = 0; i < found; i++)
{
    pos += hits[i].ToString();
    if (i < found - 1) pos += ",";
}

return $"matches={found}|collisions={collisions}|positions={pos}|patHash={patHash}";
