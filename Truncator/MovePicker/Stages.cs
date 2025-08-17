public enum Stage
{
    TTMove,
    GenerateCaptures,
    GoodCaptures,
    GenerateQuiets,
    Quiets,
    BadCaptures,

    Done,
}

public interface PickerType { }
public struct PVSPicker : PickerType { }
public struct QSPicker : PickerType { }
public struct EvasionPicker : PickerType { }
