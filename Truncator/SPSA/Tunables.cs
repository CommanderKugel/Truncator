
#pragma warning disable CA2211 // Non-constant fields should not be visible
public static class Tunables
{
    /* new(<Name>, <Default>, <Min>, <Max>, <C_end=Max/20>, <R_end=0.002>) */

    // Aspiration Windows
    public static SpsaValue AspDelta = new("AspDelta", 30, 5, 50);

    // Reverse Futility Pruning
    public static SpsaValue RfpDepth = new("RfpDepth", 5, 1, 16);
    public static SpsaValue RfpMult = new("RfpMult", 75, 25, 150);
    public static SpsaValue RfpMargin = new("RfpMargin", 0, -50, 250);

    // Razoring
    public static SpsaValue RazoringDepth = new("RazoringDepth", 4, 1, 16);
    public static SpsaValue RazoringMult = new("RazoringMult", 300, 1, 500);
    public static SpsaValue RazoringMargin = new("RazoringMargin", 0, -50, 250);

    // Null Move Pruning
    public static SpsaValue NmpBaseReduction = new("NmpBaseReduction", 3, 2, 6);
    public static SpsaValue NmpDepthDivisor = new("NmpDepthDivisor", 6, 3, 9);
    public static SpsaValue NmpEvalDivisor = new("NmpEvalDivisor", 256, 64, 512);

    // Futility Pruning
    public static SpsaValue FpDepth = new("FpDepth", 4, 1, 16);
    public static SpsaValue FpMargin = new("FpMargin", 0, -50, 250);
    public static SpsaValue FpMult = new("FpMult", 150, 1, 250);
    
    // Late Move Pruning
    public static SpsaValue LmpDepth = new("LmpDepth", 4, 1, 16);
    public static SpsaValue LmpBase = new("LmpBase", 2, 1, 32);

    // History Pruning
    public static SpsaValue HpDepth = new("HpDepth", 5, 1, 16);
    public static SpsaValue HpBase = new("HpBase", 0, -256, 256);
    public static SpsaValue HpLinMult = new("HpLinMult", 15, 1, 64);
    public static SpsaValue HpSqrMult = new("HpSqrMult", 9, 1, 64);

    // Static Exchange Evaluation
    public static SpsaValue SEENoisyMult = new("SEENoisyMult", -150, -256, -1);
    public static SpsaValue SEEQuietMult = new("SEEQuietMult", -25, 128, -1);
    public static SpsaValue SEEQsThreshold = new("SEEQsThreshold", 0, -256, 128);

    // Singular Extensions
    public static SpsaValue SEBetaDepthMult = new("SEBetaDepthMult", 2, 1, 16);
    public static SpsaValue SEDoubleMargin = new("SEDoubleMargin", 12, 1, 128);

    // Late Move Reductions
    public static SpsaValue LmrBaseBase = new("LmrBaseBase", 0, 0, 1024 * 4);
    public static SpsaValue LmrBaseMult = new("LmrBaseMult", 1024, 1, 1024 * 4);
    public static SpsaValue LmrHistDiv = new("LmrHistDiv", 341, 1, 1024);

    // Correction History
    public static SpsaValue PawnCorrhistWeight = new("PawnCorrhistWeight", 16, 1, 64);
    public static SpsaValue NpCorrhistWeight = new("NpCorrhistWeight", 12, 1, 64);
    public static SpsaValue MinorCorrhistWeight = new("MinorCorrhistWeight", 12, 1, 64);
    public static SpsaValue MajorCorrhistWeight = new("MajorCorrhistWeight", 12, 1, 64);
    public static SpsaValue ThreatCorrhistWeight = new("ThreatCorrhistWeight", 12, 1, 64);
    public static SpsaValue PrevPieceCorrhistWeight = new("PrevPieceCorrhistWeight", 12, 1, 64);

}
#pragma warning restore CA2211 // Non-constant fields should not be visible

