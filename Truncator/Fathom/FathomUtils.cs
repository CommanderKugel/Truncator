
/*
The following code was copied 1:1 from fathom source code and updated to be C# compatible
https://github.com/jdart1/Fathom/blob/master/src/tbprobe.h
*/

public static partial class FathomDll
{
    private const int
    TB_PROMOTES_NONE = 0,
    TB_PROMOTES_QUEEN = 1,
    TB_PROMOTES_ROOK = 2,
    TB_PROMOTES_BISHOP = 3,
    TB_PROMOTES_KNIGHT = 4,

    TB_RESULT_WDL_MASK = 0x0000000F,
    TB_RESULT_TO_MASK = 0x000003F0,
    TB_RESULT_FROM_MASK = 0x0000FC00,
    TB_RESULT_PROMOTES_MASK = 0x00070000,
    TB_RESULT_EP_MASK = 0x00080000;
    private const uint
    TB_RESULT_DTZ_MASK = 0xFFF00000;

    private const int
    TB_RESULT_WDL_SHIFT = 0,
    TB_RESULT_TO_SHIFT = 4,
    TB_RESULT_FROM_SHIFT = 10,
    TB_RESULT_PROMOTES_SHIFT = 16,
    TB_RESULT_EP_SHIFT = 19,
    TB_RESULT_DTZ_SHIFT = 20;

    public static readonly int TB_RESULT_CHECKMATE = TB_SET_WDL(0, (int)TbResult.TbWin);
    public static readonly int TB_RESULT_STALEMATE = TB_SET_WDL(0, (int)TbResult.TbDraw);
    public const uint TB_RESULT_FAILED = 0xFFFFFFFF;



    private static int TB_GET_WDL(int _res) => (((_res) & TB_RESULT_WDL_MASK) >> TB_RESULT_WDL_SHIFT);

    private static int TB_GET_TO(int _res) => (((_res) & TB_RESULT_TO_MASK) >> TB_RESULT_TO_SHIFT);

    private static int TB_GET_FROM(int _res) => (((_res) & TB_RESULT_FROM_MASK) >> TB_RESULT_FROM_SHIFT);

    private static int TB_GET_PROMOTES(int _res) => (((_res) & TB_RESULT_PROMOTES_MASK) >> TB_RESULT_PROMOTES_SHIFT);

    private static int TB_GET_EP(int _res) => (((_res) & TB_RESULT_EP_MASK) >> TB_RESULT_EP_SHIFT);

    private static int TB_GET_DTZ(int _res) => (int)(((_res) & TB_RESULT_DTZ_MASK) >> TB_RESULT_DTZ_SHIFT);


    private static int TB_SET_WDL(int _res, int _wdl) => (((_res) & ~TB_RESULT_WDL_MASK) | (((_wdl) << TB_RESULT_WDL_SHIFT) & TB_RESULT_WDL_MASK));

    private static int TB_SET_TO(int _res, int _to) => (((_res) & ~TB_RESULT_TO_MASK) | (((_to) << TB_RESULT_TO_SHIFT) & TB_RESULT_TO_MASK));

    private static int TB_SET_FROM(int _res, int _from) => (((_res) & ~TB_RESULT_FROM_MASK) | (((_from) << TB_RESULT_FROM_SHIFT) & TB_RESULT_FROM_MASK));

    private static int TB_SET_PROMOTES(int _res, int _promotes) => (((_res) & ~TB_RESULT_PROMOTES_MASK) | (((_promotes) << TB_RESULT_PROMOTES_SHIFT) & TB_RESULT_PROMOTES_MASK));

    private static int TB_SET_EP(int _res, int _ep) => (((_res) & ~TB_RESULT_EP_MASK) | (((_ep) << TB_RESULT_EP_SHIFT) & TB_RESULT_EP_MASK));

    private static int TB_SET_DTZ(int _res, int _dtz) => (int)(((_res) & ~TB_RESULT_DTZ_MASK) | (((_dtz) << TB_RESULT_DTZ_SHIFT) & TB_RESULT_DTZ_MASK));

}