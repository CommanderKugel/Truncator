
#pragma warning disable CA2211 // Non-constant fields should not be visible
public static class Tunables
{
    /* new(<Name>, <Default>, <Min>, <Max>, <C_end=Max/20>, <R_end=0.002>) */

    // Aspiration Windows
    public static SpsaValue AspDelta = new("AspDelta", 30, 5, 50);
    public static SpsaValue AspWidenFactor = new("AspWidenFactor", 810, 256, 4096);
    public static SpsaValue AspDeltaGrowthFactor = new("AspDeltaGrowthFactor", 631, 256, 4096);

    // Reverse Futility Pruning
    public static SpsaValue RfpDepth = new("RfpDepth", 8, 1, 16);
    public static SpsaValue RfpMult = new("RfpMult", 44, 25, 150);
    public static SpsaValue RfpMargin = new("RfpMargin", 33, -50, 250);

    // Razoring
    public static SpsaValue RazoringDepth = new("RazoringDepth", 3, 1, 16);
    public static SpsaValue RazoringMult = new("RazoringMult", 277, 1, 500);
    public static SpsaValue RazoringMargin = new("RazoringMargin", -7, -50, 250);

    // Null Move Pruning
    public static SpsaValue NmpBaseReduction = new("NmpBaseReduction", 4, 2, 6);
    public static SpsaValue NmpDepthDivisor = new("NmpDepthDivisor", 6, 3, 9);
    public static SpsaValue NmpEvalDivisor = new("NmpEvalDivisor", 291, 64, 512);

    // Probcut
    public static SpsaValue ProbuctMinDepth = new("ProbuctMinDepth", 6, 1, 16);
    public static SpsaValue ProbcutBetaMargin = new("ProbCutBetaMargin", 248, 1, 512);
    public static SpsaValue ProbcutBaseReduction = new("ProbcutBaseReduction", 6, 1, 16);

    // Futility Pruning
    public static SpsaValue FpDepth = new("FpDepth", 6, 1, 16);
    public static SpsaValue FpMargin = new("FpMargin", 11, -50, 250);
    public static SpsaValue FpMult = new("FpMult", 155, 1, 250);

    // Late Move Pruning
    public static SpsaValue LmpDepth = new("LmpDepth", 4, 1, 16);
    public static SpsaValue LmpBase = new("LmpBase", 5, 1, 32);

    // History Pruning
    public static SpsaValue HpDepth = new("HpDepth", 6, 1, 16);
    public static SpsaValue HpBase = new("HpBase", 8, -256, 256);
    public static SpsaValue HpLinMult = new("HpLinMult", 3, 1, 64);
    public static SpsaValue HpSqrMult = new("HpSqrMult", 8, 1, 64);

    // Static Exchange Evaluation
    public static SpsaValue SEENoisyMult = new("SEENoisyMult", -150, -256, -1);
    public static SpsaValue SEEQuietMult = new("SEEQuietMult", -25, -128, -1);

    public static SpsaValue SEEPvsBadNoisyThreshold = new("SEEPvsBadNoisyThreshold", -8, -256, 256);
    public static SpsaValue SEEQSBadNoisyThreshold = new("SEEQSBadNoisyThreshold", 24, -256, 256);
    public static SpsaValue SEEQsThreshold = new("SEEQsThreshold", -5, -256, 256);

    public static SpsaValue SEEPawnMaterial = new("SEEPawnMaterial", 95, 1, 256);
    public static SpsaValue SEEKnightMaterial = new("SEEKnightMaterial", 425, 256, 768);
    public static SpsaValue SEEBishopMaterial = new("SEEBishopMaterial", 468, 256, 768);
    public static SpsaValue SEERookMaterial = new("SEERookMaterial", 604, 256, 1024);
    public static SpsaValue SEEQueenMaterial = new("SEEQueenMaterial", 1496, 768, 2048);

    // Singular Extensions
    public static SpsaValue SEBetaDepthMargin = new("SEBetaDepthMargin", 11, 1, 16);
    public static SpsaValue SEDoubleMargin = new("SEDoubleMargin", 1, 1, 128);

    // Late Move Reductions
    public static SpsaValue LmrBaseBase = new("LmrBaseBase", 267, 0, 1024 * 4);
    public static SpsaValue LmrBaseMult = new("LmrBaseMult", 1022, 1, 1024 * 4);
    public static SpsaValue LmrHistDiv = new("LmrHistDiv", 59, 1, 1024);

    // Quiet Move History
    public static SpsaValue ButterflyDiv = new("ButterflyDiv", 575, 256, 4096);
    public static SpsaValue Conthist1PlyDiv = new("Conthist1PlyDiv", 1635, 256, 4096);
    public static SpsaValue Conthist2PlyDiv = new("Conthist2PlyDiv", 1385, 256, 4096);

    public static SpsaValue ButterflySearchMult = new("ButterflySearchMult", 709, 1, 4096);
    public static SpsaValue Conthist1plySearchMult = new("Conthist1plySearchMult", 819, -256, 4096);
    public static SpsaValue Conthist2plySearchMult = new("Conthist2plySearchMult", 713, -256, 4096);

    // Correction History
    public static SpsaValue CorrhistDelta = new("CorrhistDelta", 118, 1, 512);
    public static SpsaValue CorrhistFinalDiv = new("CorrhistFinalDiv", 2440, 256, 4096);

    public static SpsaValue PawnCorrhistWeight = new("PawnCorrhistWeight", 113, 1, 256);
    public static SpsaValue NpCorrhistWeight = new("NpCorrhistWeight", 115, 1, 256);
    public static SpsaValue MinorCorrhistWeight = new("MinorCorrhistWeight", 142, 1, 256);
    public static SpsaValue MajorCorrhistWeight = new("MajorCorrhistWeight", 127, 1, 256);
    public static SpsaValue ThreatCorrhistWeight = new("ThreatCorrhistWeight", 127, 1, 256);
    public static SpsaValue PrevPieceCorrhistWeight = new("PrevPieceCorrhistWeight", 111, 1, 256);

    public static SpsaValue PawnCorrhistDiv = new("PawnCorrhistDiv", 1291, 256, 4096);
    public static SpsaValue NpCorrhistDiv = new("NpCorrhistDiv", 1269, 256, 4096);
    public static SpsaValue MinorCorrhistDiv = new("MinorCorrhistDiv", 987, 256, 4096);
    public static SpsaValue MajorCorrhistDiv = new("MajorCorrhistDiv", 818, 256, 4096);
    public static SpsaValue ThreatCorrhistDiv = new("ThreatCorrhistDiv", 1174, 256, 4096);
    public static SpsaValue PrevPieceCorrhistDiv = new("PrevPieceCorrhistDiv", 766, 256, 4096);

}
#pragma warning restore CA2211 // Non-constant fields should not be visible

