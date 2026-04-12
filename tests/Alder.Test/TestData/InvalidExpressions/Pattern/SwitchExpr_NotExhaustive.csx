// Switch expression without discard — not exhaustive (warning in Roslyn, runtime exception if no match)
int x = 99;
return x switch
{
    1 => "one",
    2 => "two"
};
