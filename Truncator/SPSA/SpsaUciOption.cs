using System.Diagnostics;
using System.Reflection;

/// <summary>
/// https://github.com/AndyGrant/OpenBench/wiki/SPSA-Tuning-Workloads
/// </summary>
public static class SpsaUciOption
{

    public static Dictionary<string, FieldInfo> SpsaDict = null;

    /// <summary>
    /// Fills the SpsaDict with all the fields of the 'Tunables' class
    /// for easier access and modification by Ident
    /// (this is how you get a reference to a static value type)
    /// </summary>
    public static void CollectOptions()
    {
        SpsaDict = [];

        foreach (var field in typeof(Tunables).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            SpsaDict[field.Name] = field;
        }
    }

    /// <summary>
    /// Prints all SPSA tunable values to the Console
    /// and exposes them as UCI options.
    /// Changes to values are set via UCI options
    /// </summary>
    public static void PrintOptionsToUCI()
    {
        if (SpsaDict == null)
        {
            CollectOptions();
        }

        foreach (var field in SpsaDict.Values)
        {
            Console.WriteLine(field.GetValue(null));
        }
    }

    /// <summary>
    /// Changes the value of a static Spsa tunable value
    /// </summary>
    public static void ChangeField(string Name, int Value)
    {
        // try and get the Value we want to change

        if (!SpsaDict.TryGetValue(Name, out var field))
        {
            Console.WriteLine($"info string field {Name} not found! cant update its value.");
            return;
        }

        // this finds a static value using its Identifier.
        // thus there is totally absurd syntax incoming.
        // dont do this if you dont have to, leave while you still can.
        // i probably didnt need to do this too, there will always be an alternative
        // BUT I WANTED TO MWAHAHAHAHAHAA

        // find the static Struct 
        // and its field 'Value'

        object? copy = field.GetValue(null);
        if (copy == null)
        {
            Console.WriteLine($"info string field {Name} not found! cant update its value.");
            return;
        }

        FieldInfo? subfield = copy
            .GetType()
            .GetField("Value", BindingFlags.Public | BindingFlags.Instance);

        if (subfield == null)
        {
            Console.WriteLine($"info string field {Name} not found! cant update its value.");
            return;
        }

        // set the 'Value' fields new value on the copy
        // set the statuc structs new value from the copy

        subfield.SetValue(copy, Value);
        field.SetValue(null, copy);

        Console.WriteLine($"info string set {Name} to {Value}");
    }


    /// <summary>
    /// Helper Method for setting up an OB spsa tune.
    /// Finds all tunable values and prints the input for the "SPSA input" box
    /// when creating a tuning workload
    /// https://commanderkugel.pythonanywhere.com/tune/new/
    /// </summary>
    public static void PrintValuesInOBFormat()
    {
        if (SpsaDict == null)
        {
            CollectOptions();
        }

        foreach (var field in SpsaDict.Values)
        {
            var value = field.GetValue(null);
            var ToOBFormat = typeof(SpsaValue).GetMethod("ToOBFormat", BindingFlags.Public | BindingFlags.Static);
            Console.WriteLine(ToOBFormat.Invoke(null, [value]));
        }
    }

}