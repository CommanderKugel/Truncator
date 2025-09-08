
#pragma warning disable CA2211 // Non-constant fields should not be visible
public static class Tunables
{
    /* new(<Name>, <Default>, <Min>, <Max>, <C_end=Max/20>, <R_end=0.002>) */

    // Aspiration Windows
    public static SpsaValue AspDelta = new("AspDelta", 30, 5, 50);

    // Reverse Futility Pruning
    public static SpsaValue RfpDepth = new("RfpDepth", 6, 1, 16);
    public static SpsaValue RfpMult = new("RfpMult", 65, 25, 150);
    public static SpsaValue RfpMargin = new("RfpMargin", 24, -50, 250);

    // Razoring
    public static SpsaValue RazoringDepth = new("RazoringDepth", 3, 1, 16);
    public static SpsaValue RazoringMult = new("RazoringMult", 263, 1, 500);
    public static SpsaValue RazoringMargin = new("RazoringMargin", -2, -50, 250);

    // Null Move Pruning
    public static SpsaValue NmpBaseReduction = new("NmpBaseReduction", 4, 2, 6);
    public static SpsaValue NmpDepthDivisor = new("NmpDepthDivisor", 6, 3, 9);
    public static SpsaValue NmpEvalDivisor = new("NmpEvalDivisor", 273, 64, 512);

    // Probcut
    public static SpsaValue ProbuctMinDepth = new("ProbuctMinDepth", 5, 1, 16);
    public static SpsaValue ProbcutBetaMargin = new("ProbCutBetaMargin", 250, 1, 512);
    public static SpsaValue ProbcutBaseReduction = new("ProbcutBaseReduction", 6, 1, 16);

    // Futility Pruning
    public static SpsaValue FpDepth = new("FpDepth", 4, 1, 16);
    public static SpsaValue FpMargin = new("FpMargin", 19, -50, 250);
    public static SpsaValue FpMult = new("FpMult", 145, 1, 250);

    // Late Move Pruning
    public static SpsaValue LmpDepth = new("LmpDepth", 4, 1, 16);
    public static SpsaValue LmpBase = new("LmpBase", 3, 1, 32);

    // History Pruning
    public static SpsaValue HpDepth = new("HpDepth", 6, 1, 16);
    public static SpsaValue HpBase = new("HpBase", -9, -256, 256);
    public static SpsaValue HpLinMult = new("HpLinMult", 5, 1, 64);
    public static SpsaValue HpSqrMult = new("HpSqrMult", 7, 1, 64);

    // Static Exchange Evaluation
    public static SpsaValue SEENoisyMult = new("SEENoisyMult", -149, -256, -1);
    public static SpsaValue SEEQuietMult = new("SEEQuietMult", -25, -128, -1);

    public static SpsaValue SEEPvsBadNoisyThreshold = new("SEEPvsBadNoisyThreshold", 0, -256, 256);
    public static SpsaValue SEEQSBadNoisyThreshold = new("SEEQSBadNoisyThreshold", 0, -256, 256);
    public static SpsaValue SEEQsThreshold = new("SEEQsThreshold", 6, -256, 256);

    public static SpsaValue SEEPawnMaterial = new("SEEPawnMaterial", 100, 1, 256);
    public static SpsaValue SEEKnightMaterial = new("SEEKnightMaterial", 450, 256, 768);
    public static SpsaValue SEEBishopMaterial = new("SEEBishopMaterial", 450, 256, 768);
    public static SpsaValue SEERookMaterial = new("SEERookMaterial", 650, 256, 1024);
    public static SpsaValue SEEQueenMaterial = new("SEEQueenMaterial", 1250, 768, 2048);

    // Singular Extensions
    public static SpsaValue SEBetaDepthMult = new("SEBetaDepthMult", 2, 1, 16);
    public static SpsaValue SEDoubleMargin = new("SEDoubleMargin", 2, 1, 128);

    // Late Move Reductions
    public static SpsaValue LmrBaseBase = new("LmrBaseBase", 459, 0, 1024 * 4);
    public static SpsaValue LmrBaseMult = new("LmrBaseMult", 1451, 1, 1024 * 4);
    public static SpsaValue LmrHistDiv = new("LmrHistDiv", 234, 1, 1024);

    // Quiet Move History
    public static SpsaValue ButterflyDiv = new("ButterflyDiv", 1024, 256, 4096);
    public static SpsaValue Conthist1PlyDiv = new("Conthist1PlyDiv", 1024, 256, 4096);
    public static SpsaValue Conthist2PlyDiv = new("Conthist2PlyDiv", 1024, 256, 4096);

    public static SpsaValue ButterflySearchMult = new("ButterflySearchMult", 1024, 1, 4096);
    public static SpsaValue Conthist1plySearchMult = new("Conthist1plySearchMult", 0, -256, 4096);
    public static SpsaValue Conthist2plySearchMult = new("Conthist2plySearchMult", 0, -256, 4096);

    // Correction History
    public static SpsaValue CorrhistDelta = new("CorrhistDelta", 128, 1, 512);
    public static SpsaValue CorrhistFinalDiv = new("CorrhistFinalDiv", 1024, 256, 4096);

    public static SpsaValue PawnCorrhistWeight = new("PawnCorrhistWeight", 16, 1, 64);
    public static SpsaValue NpCorrhistWeight = new("NpCorrhistWeight", 12, 1, 64);
    public static SpsaValue MinorCorrhistWeight = new("MinorCorrhistWeight", 12, 1, 64);
    public static SpsaValue MajorCorrhistWeight = new("MajorCorrhistWeight", 12, 1, 64);
    public static SpsaValue ThreatCorrhistWeight = new("ThreatCorrhistWeight", 12, 1, 64);
    public static SpsaValue PrevPieceCorrhistWeight = new("PrevPieceCorrhistWeight", 12, 1, 64);

    public static SpsaValue PawnCorrhistDiv = new("PawnCorrhistDiv", 1024, 256, 4096);
    public static SpsaValue NpCorrhistDiv = new("NpCorrhistDiv", 1024, 256, 4096);
    public static SpsaValue MinorCorrhistDiv = new("MinorCorrhistDiv", 1024, 256, 4096);
    public static SpsaValue MajorCorrhistDiv = new("MajorCorrhistDiv", 1024, 256, 4096);
    public static SpsaValue ThreatCorrhistDiv = new("ThreatCorrhistDiv", 1024, 256, 4096);
    public static SpsaValue PrevPieceCorrhistDiv = new("PrevPieceCorrhistDiv", 1024, 256, 4096);

}
#pragma warning restore CA2211 // Non-constant fields should not be visible

