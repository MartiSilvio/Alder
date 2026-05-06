// Relational pattern is only valid for numeric types, not bool
bool b = true;
return b switch
{
    > false => 1,
    _ => 0
};
