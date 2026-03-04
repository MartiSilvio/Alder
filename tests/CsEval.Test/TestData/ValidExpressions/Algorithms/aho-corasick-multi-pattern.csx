// Aho-Corasick multi-pattern matching with explicit trie/failure links.
// Uses fixed lowercase alphabet [a-z] and bitmask outputs.

var patterns = new[] { "he", "she", "his", "hers", "is", "her" };
var text = "ahishershehishersishe";
var alphabet = "abcdefghijklmnopqrstuvwxyz";

var alpha = 26;
var maxStates = 128;

// next[state * alpha + ch] = next state, -1 if absent
var next = new int[maxStates * alpha];
for (var i = 0; i < next.Length; i++) next[i] = -1;

var fail = new int[maxStates];
var outMask = new int[maxStates];
for (var i = 0; i < maxStates; i++)
{
    fail[i] = 0;
    outMask[i] = 0;
}

var states = 1; // root = 0

// Build trie
for (var p = 0; p < patterns.Length; p++)
{
    var s = 0;
    var pat = patterns[p];
    for (var i = 0; i < pat.Length; i++)
    {
        var ch = alphabet.IndexOf(pat.Substring(i, 1));
        var idx = s * alpha + ch;
        if (next[idx] == -1)
        {
            next[idx] = states;
            states++;
        }
        s = next[idx];
    }
    outMask[s] |= (1 << p);
}

// BFS for failure links
var q = new int[maxStates];
var head = 0;
var tail = 0;

for (var ch = 0; ch < alpha; ch++)
{
    var s = next[ch];
    if (s != -1)
    {
        fail[s] = 0;
        q[tail++] = s;
    }
    else
    {
        next[ch] = 0; // root fallback
    }
}

while (head < tail)
{
    var r = q[head++];
    for (var ch = 0; ch < alpha; ch++)
    {
        var idx = r * alpha + ch;
        var s = next[idx];
        if (s != -1)
        {
            q[tail++] = s;
            var f = fail[r];
            var fidx = f * alpha + ch;
            fail[s] = next[fidx];
            outMask[s] |= outMask[fail[s]];
        }
        else
        {
            next[idx] = next[fail[r] * alpha + ch];
        }
    }
}

// Search
var counts = new int[patterns.Length];
for (var i = 0; i < counts.Length; i++) counts[i] = 0;

var state = 0;
var transitions = 0;
for (var i = 0; i < text.Length; i++)
{
    var ch = alphabet.IndexOf(text.Substring(i, 1));
    if (ch < 0)
    {
        state = 0;
        continue;
    }

    state = next[state * alpha + ch];
    transitions++;

    var mask = outMask[state];
    if (mask != 0)
    {
        for (var p = 0; p < patterns.Length; p++)
        {
            if ((mask & (1 << p)) != 0) counts[p]++;
        }
    }
}

var totalHits = 0;
var hash = 17;
for (var i = 0; i < counts.Length; i++)
{
    totalHits += counts[i];
    hash = hash * 31 + counts[i] * (i + 3);
}

var breakdown = "";
for (var i = 0; i < patterns.Length; i++)
{
    breakdown += patterns[i] + ":" + counts[i];
    if (i < patterns.Length - 1) breakdown += ",";
}

return $"states={states}|transitions={transitions}|hits={totalHits}|hash={hash}|{breakdown}";
