namespace Alder.Binding;

[Flags]
internal enum BinderFlags
{
    None = 0,
    InLoop = 1 << 0,
    InSwitch = 1 << 1,
    InLockBody = 1 << 2,
    InFinally = 1 << 3,
    InCatch = 1 << 4,
}
