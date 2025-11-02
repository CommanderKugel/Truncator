
#pragma warning disable CA2211 // Non-constant fields should not be visible
public static class Tunables
{
    /* new(<Name>, <Default>, <Min>, <Max>, <C_end=Max/20>, <R_end=0.002>) */

    // Aspiration Windows
    public static SpsaValue AspDelta = new("AspDelta", 46, 5, 50);
    public static SpsaValue AspWidenFactor = new("AspWidenFactor", 1468, 256, 4096);
    public static SpsaValue AspDeltaGrowthFactor = new("AspDeltaGrowthFactor", 951, 256, 4096);

    // Reverse Futility Pruning
    public static SpsaValue RfpDepth = new("RfpDepth", 11, 1, 16);
    public static SpsaValue RfpMult = new("RfpMult", 50, 25, 150);
    public static SpsaValue RfpMargin = new("RfpMargin", 6, -50, 250);

    // Razoring
    public static SpsaValue RazoringDepth = new("RazoringDepth", 2, 1, 16);
    public static SpsaValue RazoringMult = new("RazoringMult", 350, 1, 500);
    public static SpsaValue RazoringMargin = new("RazoringMargin", 16, -50, 250);

    // Null Move Pruning
    public static SpsaValue NmpBaseReduction = new("NmpBaseReduction", 5, 2, 6);
    public static SpsaValue NmpDepthDivisor = new("NmpDepthDivisor", 5, 3, 9);
    public static SpsaValue NmpEvalDivisor = new("NmpEvalDivisor", 294, 64, 512);

    // Probcut
    public static SpsaValue ProbuctMinDepth = new("ProbuctMinDepth", 9, 1, 16);
    public static SpsaValue ProbcutBetaMargin = new("ProbCutBetaMargin", 231, 1, 512);
    public static SpsaValue ProbcutBaseReduction = new("ProbcutBaseReduction", 4, 1, 16);

    // Futility Pruning
    public static SpsaValue FpDepth = new("FpDepth", 6, 1, 16);
    public static SpsaValue FpMargin = new("FpMargin", 16, -50, 250);
    public static SpsaValue FpMult = new("FpMult", 156, 1, 250);

    // Late Move Pruning
    public static SpsaValue LmpDepth = new("LmpDepth", 4, 1, 16);
    public static SpsaValue LmpBase = new("LmpBase", 3, 1, 32);

    // History Pruning
    public static SpsaValue HpDepth = new("HpDepth", 5, 1, 16);
    public static SpsaValue HpBase = new("HpBase", 34, -256, 256);
    public static SpsaValue HpLinMult = new("HpLinMult", 7, 1, 64);
    public static SpsaValue HpSqrMult = new("HpSqrMult", 10, 1, 64);

    // Static Exchange Evaluation
    public static SpsaValue SEENoisyMult = new("SEENoisyMult", -149, -256, -1);
    public static SpsaValue SEEQuietMult = new("SEEQuietMult", -25, -128, -1);

    public static SpsaValue SEEPvsBadNoisyThreshold = new("SEEPvsBadNoisyThreshold", -16, -256, 256);
    public static SpsaValue SEEQSBadNoisyThreshold = new("SEEQSBadNoisyThreshold", -16, -256, 256);

    public static SpsaValue SEEPawnMaterial = new("SEEPawnMaterial", 99, 1, 256);
    public static SpsaValue SEEKnightMaterial = new("SEEKnightMaterial", 373, 256, 768);
    public static SpsaValue SEEBishopMaterial = new("SEEBishopMaterial", 440, 256, 768);
    public static SpsaValue SEERookMaterial = new("SEERookMaterial", 596, 256, 1024);
    public static SpsaValue SEEQueenMaterial = new("SEEQueenMaterial", 1150, 768, 2048);

    // Singular Extensions
    public static SpsaValue SEBetaDepthMargin = new("SEBetaDepthMargin", 10, 1, 16);
    public static SpsaValue SEDoubleMargin = new("SEDoubleMargin", 3, 1, 128);

    // Late Move Reductions
    public static SpsaValue LmrBaseBase = new("LmrBaseBase", 315, 0, 1024 * 4);
    public static SpsaValue LmrBaseMult = new("LmrBaseMult", 1477, 1, 1024 * 4);
    public static SpsaValue LmrHistDiv = new("LmrHistDiv", 112, 1, 1024);

    // Quiet Move History
    public static SpsaValue ButterflyDiv = new("ButterflyDiv", 281, 256, 4096);
    public static SpsaValue Conthist1PlyDiv = new("Conthist1PlyDiv", 1969, 256, 4096);
    public static SpsaValue Conthist2PlyDiv = new("Conthist2PlyDiv", 980, 256, 4096);

    public static SpsaValue ButterflySearchMult = new("ButterflySearchMult", 569, 1, 4096);
    public static SpsaValue Conthist1plySearchMult = new("Conthist1plySearchMult", 1034, -256, 4096);
    public static SpsaValue Conthist2plySearchMult = new("Conthist2plySearchMult", 666, -256, 4096);

    // Correction History
    public static SpsaValue CorrhistDelta = new("CorrhistDelta", 85, 1, 512);
    public static SpsaValue CorrhistFinalDiv = new("CorrhistFinalDiv", 2681, 256, 4096);

    public static SpsaValue PawnCorrhistWeight = new("PawnCorrhistWeight", 98, 1, 256);
    public static SpsaValue NpCorrhistWeight = new("NpCorrhistWeight", 122, 1, 256);
    public static SpsaValue MinorCorrhistWeight = new("MinorCorrhistWeight", 133, 1, 256);
    public static SpsaValue MajorCorrhistWeight = new("MajorCorrhistWeight", 149, 1, 256);
    public static SpsaValue ThreatCorrhistWeight = new("ThreatCorrhistWeight", 125, 1, 256);
    public static SpsaValue PrevPieceCorrhistWeight = new("PrevPieceCorrhistWeight", 127, 1, 256);

    public static SpsaValue PawnCorrhistDiv = new("PawnCorrhistDiv", 1273, 256, 4096);
    public static SpsaValue NpCorrhistDiv = new("NpCorrhistDiv", 927, 256, 4096);
    public static SpsaValue MinorCorrhistDiv = new("MinorCorrhistDiv", 527, 256, 4096);
    public static SpsaValue MajorCorrhistDiv = new("MajorCorrhistDiv", 986, 256, 4096);
    public static SpsaValue ThreatCorrhistDiv = new("ThreatCorrhistDiv", 1182, 256, 4096);
    public static SpsaValue PrevPieceCorrhistDiv = new("PrevPieceCorrhistDiv", 734, 256, 4096);

}
#pragma warning restore CA2211 // Non-constant fields should not be visible

