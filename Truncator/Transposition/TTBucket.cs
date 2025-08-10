
using System.Diagnostics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 16, Size = 64)]
public struct TTBucket
{

    public const int SIZE = 4;

    private TTEntry entry1, entry2, entry3, entry4;

    public unsafe TTEntry this[int i]
    {
        get
        {
            Debug.Assert(i >= 0 && i < SIZE);
            fixed (TTBucket* ptr = &this)
            {
                return *(((TTEntry*)ptr) + i);
            }
        }

        set
        {
            Debug.Assert(i >= 0 && i < SIZE);
            fixed (TTBucket* ptr = &this)
            {
                *(((TTEntry*)ptr) + i) = value;
            }
        }
    }

}