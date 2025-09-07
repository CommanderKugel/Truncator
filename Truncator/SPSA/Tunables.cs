
#pragma warning disable CA2211 // Non-constant fields should not be visible
public static class Tunables
{
    /* new(<Name>, <Default>, <Min>, <Max>, <C_end=Max/20>, <R_end=0.002>) */

    // Aspiration Windows
    public static SpsaValue AspDelta = new("AspDelta", 31, 5, 50);

    // Reverse Futility Pruning
    public static SpsaValue RfpDepth = new("RfpDepth", 7, 1, 16);
    public static SpsaValue RfpMult = new("RfpMult", 62, 25, 150);
    public static SpsaValue RfpMargin = new("RfpMargin", 8, -50, 250);

    // Razoring
    public static SpsaValue RazoringDepth = new("RazoringDepth", 3, 1, 16);
    public static SpsaValue RazoringMult = new("RazoringMult", 262, 1, 500);
    public static SpsaValue RazoringMargin = new("RazoringMargin", 7, -50, 250);

    // Null Move Pruning
    public static SpsaValue NmpBaseReduction = new("NmpBaseReduction", 4, 2, 6);
    public static SpsaValue NmpDepthDivisor = new("NmpDepthDivisor", 6, 3, 9);
    public static SpsaValue NmpEvalDivisor = new("NmpEvalDivisor", 230, 64, 512);

    // Probcut
    public static SpsaValue ProbcutBetaMargin = new("ProbcutBetaMargin", 260, 1, 512);
    public static SpsaValue ProbcutDepth = new("ProbcutDepth", 6, 1, 16);
    public static SpsaValue ProbcutBaseReduction = new("ProbcutBaseReduction", 5, 1, 16);

    // Futility Pruning
    public static SpsaValue FpDepth = new("FpDepth", 4, 1, 16);
    public static SpsaValue FpMargin = new("FpMargin", 29, -50, 250);
    public static SpsaValue FpMult = new("FpMult", 133, 1, 250);

    // Late Move Pruning
    public static SpsaValue LmpDepth = new("LmpDepth", 4, 1, 16);
    public static SpsaValue LmpBase = new("LmpBase", 3, 1, 32);

    // History Pruning
    public static SpsaValue HpDepth = new("HpDepth", 7, 1, 16);
    public static SpsaValue HpBase = new("HpBase", -16, -256, 256);
    public static SpsaValue HpLinMult = new("HpLinMult", 6, 1, 64);
    public static SpsaValue HpSqrMult = new("HpSqrMult", 5, 1, 64);

    // Static Exchange Evaluation
    public static SpsaValue SEEBadCaptureMargin = new("SEEPvsBadcaptMargin", 14, -256, 256);
    public static SpsaValue SEENoisyMult = new("SEENoisyMult", -149, -256, -1);
    public static SpsaValue SEEQuietMult = new("SEEQuietMult", -25, -128, -1);
    public static SpsaValue SEEQsThreshold = new("SEEQsThreshold", 12, -256, 128);

    public static SpsaValue SEEMaterialPawn = new("SEEMaterialPawn", 109, 1, 256);
    public static SpsaValue SEEMaterialKnight = new("SEEMaterialKnight", 490, 256, 768);
    public static SpsaValue SEEMaterialBishop = new("SEEMaterialBishop", 410, 256, 768);
    public static SpsaValue SEEMaterialRook = new("SEEMaterialRook", 589, 512, 1024);
    public static SpsaValue SEEMaterialQueen = new("SEEMaterialQueen", 1329, 768, 2048);

    // Singular Extensions
    public static SpsaValue SEBetaDepthMult = new("SEBetaDepthMult", 2, 1, 16);
    public static SpsaValue SEDoubleMargin = new("SEDoubleMargin", 2, 1, 128);

    // Late Move Reductions
    public static SpsaValue LmrBaseBase = new("LmrBaseBase", 455, 0, 1024 * 4);
    public static SpsaValue LmrBaseMult = new("LmrBaseMult", 1606, 1, 1024 * 4);
    public static SpsaValue LmrHistDiv = new("LmrHistDiv", 274, 1, 1024);

    // History in Search & Updates
    public static SpsaValue ButterflySearchMult = new("ButterflySearchMult", 802, 0, 4096);
    public static SpsaValue Conthist1SearchMult = new("Conthist1SearchMult", 769, -128, 4096);
    public static SpsaValue Conthist2SearchMult = new("Conthist2SearchMult", 417, -128, 4096);

    public static SpsaValue HistUpdateMult = new("HistUpdateMult", 1537, 512, 4096);
    public static SpsaValue ButterflyDiv = new("ButterflyDiv", 588, 512, 4096);
    public static SpsaValue ContHistDiv = new("ContHistDiv", 976, 512, 4096);

    // Correction History
    public static SpsaValue CorrhistDivFinal = new("CorrhistDivFinal", 514, 512, 4096);

    public static SpsaValue PawnCorrhistWeight = new("PawnCorrhistWeight", 19, 1, 64);
    public static SpsaValue NpCorrhistWeight = new("NpCorrhistWeight", 19, 1, 64);
    public static SpsaValue MinorCorrhistWeight = new("MinorCorrhistWeight", 14, 1, 64);
    public static SpsaValue MajorCorrhistWeight = new("MajorCorrhistWeight", 11, 1, 64);
    public static SpsaValue ThreatCorrhistWeight = new("ThreatCorrhistWeight", 21, 1, 64);
    public static SpsaValue PrevPieceCorrhistWeight = new("PrevPieceCorrhistWeight", 17, 1, 64);

    public static SpsaValue PawnCorrhistDiv = new("PawnCorrhistDiv", 1290, 512, 4096);
    public static SpsaValue NpCorrhistDiv = new("NpCorrhistDiv", 1021, 512, 4096);
    public static SpsaValue MinorCorrhistDiv = new("MinorCorrhistDiv", 1072, 512, 4096);
    public static SpsaValue MajorCorrhistDiv = new("MajorCorrhistDiv", 1206, 512, 4096);
    public static SpsaValue ThreatCorrhistDiv = new("ThreatCorrhistDiv", 1150, 512, 4096);
    public static SpsaValue PrevPieceCorrhistDiv = new("PrevPieceCorrhistDiv", 1320, 512, 4096);

}
#pragma warning restore CA2211 // Non-constant fields should not be visible

