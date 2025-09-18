
#include "tbprobe.h"
#include "stdendian.h"

__declspec(dllexport) int get_largest() {
    return TB_LARGEST;
}

__declspec(dllexport) int tb_init_(const char *_path) {
    return tb_init(_path);
}

__declspec(dllexport) int tb_free_(void) {
    tb_free();
    return 0;
}

__declspec(dllexport) int tb_probe_wdl_(
    uint64_t _white,
    uint64_t _black,
    uint64_t _kings,
    uint64_t _queens,
    uint64_t _rooks,
    uint64_t _bishops,
    uint64_t _knights,
    uint64_t _pawns,
    unsigned _rule50,
    unsigned _castling,
    unsigned _ep,
    bool     _turn) 
{
    return tb_probe_wdl(
        _white,
        _black,
        _kings,
        _queens,
        _rooks,
        _bishops,
        _knights,
        _pawns,
        _rule50,
        _castling,
        _ep,
        _turn);
}

__declspec(dllexport) int tb_probe_root_(
    uint64_t _white,
    uint64_t _black,
    uint64_t _kings,
    uint64_t _queens,
    uint64_t _rooks,
    uint64_t _bishops,
    uint64_t _knights,
    uint64_t _pawns,
    unsigned _rule50,
    unsigned _castling,
    unsigned _ep,
    bool     _turn,
    unsigned *_results) {
    return tb_probe_root(
        _white,
        _black,
        _kings,
        _queens,
        _rooks,
        _bishops,
        _knights,
        _pawns,
        _rule50,
        _castling,
        _ep,
        _turn,
        _results);
}

__declspec(dllexport) int tb_probe_root_dtz_(
    uint64_t _white,
    uint64_t _black,
    uint64_t _kings,
    uint64_t _queens,
    uint64_t _rooks,
    uint64_t _bishops,
    uint64_t _knights,
    uint64_t _pawns,
    unsigned _rule50,
    unsigned _castling,
    unsigned _ep,
    bool     _turn,
    bool hasRepeated,
    bool useRule50,
    struct TbRootMoves *_results)
{
    return tb_probe_root_dtz(
        _white,
        _black,
        _kings,
        _queens,
        _rooks,
        _bishops,
        _knights,
        _pawns,
        _rule50,
        _castling,
        _ep,
        _turn,
        hasRepeated,
        useRule50,
        _results);
}

__declspec(dllexport) int tb_probe_root_wdl_(
    uint64_t _white,
    uint64_t _black,
    uint64_t _kings,
    uint64_t _queens,
    uint64_t _rooks,
    uint64_t _bishops,
    uint64_t _knights,
    uint64_t _pawns,
    unsigned _rule50,
    unsigned _castling,
    unsigned _ep,
    bool     _turn,
    bool useRule50,
    struct TbRootMoves *_results)
{
    return tb_probe_root_wdl(
        _white,
        _black,
        _kings,
        _queens,
        _rooks,
        _bishops,
        _knights,
        _pawns,
        _rule50,
        _castling,
        _ep,
        _turn,
        useRule50,
        _results);
}
