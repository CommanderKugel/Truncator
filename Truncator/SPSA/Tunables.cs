
#pragma warning disable CA2211 // Non-constant fields should not be visible
public static class Tunables
{
    /* new(<Name>, <Default>, <Min>, <Max>, <C_end=Max/20>, <R_end=0.002>) */

    // Aspiration Windows
    public static SpsaValue AspDelta = new("AspDelta", 31, 5, 50);
    public static SpsaValue AspWidenFactor = new("AspWidenFactor", 1024, 256, 4096);
    public static SpsaValue AspDeltaGrowthFactor = new("AspDeltaGrowthFactor", 1024, 256, 4096);

    // Reverse Futility Pruning
    public static SpsaValue RfpDepth = new("RfpDepth", 6, 1, 16);
    public static SpsaValue RfpMult = new("RfpMult", 45, 25, 150);
    public static SpsaValue RfpMargin = new("RfpMargin", 18, -50, 250);

    // Razoring
    public static SpsaValue RazoringDepth = new("RazoringDepth", 3, 1, 16);
    public static SpsaValue RazoringMult = new("RazoringMult", 272, 1, 500);
    public static SpsaValue RazoringMargin = new("RazoringMargin", -11, -50, 250);

    // Null Move Pruning
    public static SpsaValue NmpBaseReduction = new("NmpBaseReduction", 4, 2, 6);
    public static SpsaValue NmpDepthDivisor = new("NmpDepthDivisor", 6, 3, 9);
    public static SpsaValue NmpEvalDivisor = new("NmpEvalDivisor", 283, 64, 512);

    // Probcut
    public static SpsaValue ProbuctMinDepth = new("ProbuctMinDepth", 6, 1, 16);
    public static SpsaValue ProbcutBetaMargin = new("ProbCutBetaMargin", 250, 1, 512);
    public static SpsaValue ProbcutBaseReduction = new("ProbcutBaseReduction", 6, 1, 16);

    // Futility Pruning
    public static SpsaValue FpDepth = new("FpDepth", 5, 1, 16);
    public static SpsaValue FpMargin = new("FpMargin", 18, -50, 250);
    public static SpsaValue FpMult = new("FpMult", 148, 1, 250);

    // Late Move Pruning
    public static SpsaValue LmpDepth = new("LmpDepth", 4, 1, 16);
    public static SpsaValue LmpBase = new("LmpBase", 3, 1, 32);

    // History Pruning
    public static SpsaValue HpDepth = new("HpDepth", 6, 1, 16);
    public static SpsaValue HpBase = new("HpBase", -5, -256, 256);
    public static SpsaValue HpLinMult = new("HpLinMult", 5, 1, 64);
    public static SpsaValue HpSqrMult = new("HpSqrMult", 6, 1, 64);

    // Static Exchange Evaluation
    public static SpsaValue SEENoisyMult = new("SEENoisyMult", -149, -256, -1);
    public static SpsaValue SEEQuietMult = new("SEEQuietMult", -25, -128, -1);

    public static SpsaValue SEEPvsBadNoisyThreshold = new("SEEPvsBadNoisyThreshold", -14, -256, 256);
    public static SpsaValue SEEQSBadNoisyThreshold = new("SEEQSBadNoisyThreshold", 17, -256, 256);
    public static SpsaValue SEEQsThreshold = new("SEEQsThreshold", -13, -256, 256);

    public static SpsaValue SEEPawnMaterial = new("SEEPawnMaterial", 99, 1, 256);
    public static SpsaValue SEEKnightMaterial = new("SEEKnightMaterial", 406, 256, 768);
    public static SpsaValue SEEBishopMaterial = new("SEEBishopMaterial", 460, 256, 768);
    public static SpsaValue SEERookMaterial = new("SEERookMaterial", 565, 256, 1024);
    public static SpsaValue SEEQueenMaterial = new("SEEQueenMaterial", 1376, 768, 2048);

    // Singular Extensions
    public static SpsaValue SEBetaDepthMargin = new("SEBetaDepthMargin", 12, 1, 16);
    public static SpsaValue SEDoubleMargin = new("SEDoubleMargin", 4, 1, 128);

    // Late Move Reductions
    public static SpsaValue LmrBaseBase = new("LmrBaseBase", 343, 0, 1024 * 4);
    public static SpsaValue LmrBaseMult = new("LmrBaseMult", 1251, 1, 1024 * 4);
    public static SpsaValue LmrHistDiv = new("LmrHistDiv", 185, 1, 1024);

    // Quiet Move History
    public static SpsaValue ButterflyDiv = new("ButterflyDiv", 419, 256, 4096);
    public static SpsaValue Conthist1PlyDiv = new("Conthist1PlyDiv", 1117, 256, 4096);
    public static SpsaValue Conthist2PlyDiv = new("Conthist2PlyDiv", 1024, 256, 4096);

    public static SpsaValue ButterflySearchMult = new("ButterflySearchMult", 1049, 1, 4096);
    public static SpsaValue Conthist1plySearchMult = new("Conthist1plySearchMult", 436, -256, 4096);
    public static SpsaValue Conthist2plySearchMult = new("Conthist2plySearchMult", 349, -256, 4096);

    // Correction History
    public static SpsaValue CorrhistDelta = new("CorrhistDelta", 160, 1, 512);
    public static SpsaValue CorrhistFinalDiv = new("CorrhistFinalDiv", 2048, 256, 4096);

    public static SpsaValue PawnCorrhistWeight = new("PawnCorrhistWeight", 13 * 8, 1, 256);
    public static SpsaValue NpCorrhistWeight = new("NpCorrhistWeight", 16 * 8, 1, 256);
    public static SpsaValue MinorCorrhistWeight = new("MinorCorrhistWeight", 19 * 8, 1, 256);
    public static SpsaValue MajorCorrhistWeight = new("MajorCorrhistWeight", 17 * 8, 1, 256);
    public static SpsaValue ThreatCorrhistWeight = new("ThreatCorrhistWeight", 14 * 8, 1, 256);
    public static SpsaValue PrevPieceCorrhistWeight = new("PrevPieceCorrhistWeight", 13 * 8, 1, 256);

    public static SpsaValue PawnCorrhistDiv = new("PawnCorrhistDiv", 1287, 256, 4096);
    public static SpsaValue NpCorrhistDiv = new("NpCorrhistDiv", 1246, 256, 4096);
    public static SpsaValue MinorCorrhistDiv = new("MinorCorrhistDiv", 1007, 256, 4096);
    public static SpsaValue MajorCorrhistDiv = new("MajorCorrhistDiv", 899, 256, 4096);
    public static SpsaValue ThreatCorrhistDiv = new("ThreatCorrhistDiv", 981, 256, 4096);
    public static SpsaValue PrevPieceCorrhistDiv = new("PrevPieceCorrhistDiv", 942, 256, 4096);

}
#pragma warning restore CA2211 // Non-constant fields should not be visible

