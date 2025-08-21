using System.Reflection;

public static class SpsaUciOption
{

    private static Dictionary<string, FieldInfo> SpsaDict = [];

    /// <summary>
    /// Fills the SpsaDict with all the fields of the 'Tunables' class
    /// for easier access and modification by Ident
    /// (this is how you get a reference to a static value type)
    /// </summary>
    public static void CollectOptions()
    {
        foreach (var field in typeof(Tunables).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            SpsaDict[field.Name] = field;
        }
    }


    /// <summary>
    /// Changes the value of a static Spsa tunable value
    /// </summary>
    public static void ChangeField(string name, int Value)
    {
        // try and get the Value we want to change

        if (!SpsaDict.TryGetValue(name, out var field))
        {
            Console.WriteLine($"info string field {name} not found! cant update its value.");
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
            Console.WriteLine($"info string field {name} not found! cant update its value.");
            return;
        }

        FieldInfo? subfield = copy
            .GetType()
            .GetField("Value", BindingFlags.Public | BindingFlags.Instance);

        if (subfield == null)
        {
            Console.WriteLine($"info string field {name} not found! cant update its value.");
            return;
        }

        // set the 'Value' fields new value on the copy
        // set the statuc structs new value from the copy

        subfield.SetValue(copy, Value);
        field.SetValue(null, copy);

        Console.WriteLine(field.GetValue(null));
    }

}