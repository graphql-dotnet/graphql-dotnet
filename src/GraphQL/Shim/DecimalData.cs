namespace GraphQL;

// * DESCRIPTION TAKEN FROM MS REFERENCE SOURCE *
// https://github.com/microsoft/referencesource/blob/master/mscorlib/system/decimal.cs
// The lo, mid, hi, and flags fields contain the representation of the
// Decimal value. The lo, mid, and hi fields contain the 96-bit integer
// part of the Decimal. Bits 0-15 (the lower word) of the flags field are
// unused and must be zero; bits 16-23 contain must contain a value between
// 0 and 28, indicating the power of 10 to divide the 96-bit integer part
// by to produce the Decimal value; bits 24-30 are unused and must be zero;
// and finally bit 31 indicates the sign of the Decimal value, 0 meaning
// positive and 1 meaning negative.
internal readonly struct DecimalData
{
    public readonly uint Flags;
    public readonly uint Hi;
    public readonly uint Lo;
    public readonly uint Mid;

    internal DecimalData(uint flags, uint hi, uint lo, uint mid)
    {
        Flags = flags;
        Hi = hi;
        Lo = lo;
        Mid = mid;
    }

    internal bool Equals(in DecimalData other) => Flags == other.Flags && Hi == other.Hi && Lo == other.Lo && Mid == other.Mid;
}
