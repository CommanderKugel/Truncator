
using System.Diagnostics;
using System.Reflection;

public static class BindingHandler
{

    private static List<string> Files = new List<string>();

    public static bool UnpackContainedDll(string resourceName, string fileName)
    {
        string filePath = Path.Combine(AppContext.BaseDirectory, fileName);

        // quit if the file is already extracted

        if (File.Exists(filePath))
        {
            Debug.WriteLine($"file {filePath} already exists!");
            Files.Add(filePath);
            return true;
        }

        // first extract the assembly from the compiled program
        // then paste it into the correct file

        var asm = Assembly.GetEntryAssembly();
        Debug.WriteLine($"looking for {resourceName} in [{string.Join(", ", asm.GetManifestResourceNames())}]");
        Stream stream = asm.GetManifestResourceStream(resourceName) ?? throw new FileNotFoundException($"file not found :(");

        // then, paste the assembly in an external file
        // simply create the file anew, as it does not exist yet

        using FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        stream.CopyTo(fs);
        Console.WriteLine($"info string 'Fathom Dll at: {filePath}'");

        // remember file for later Disposal

        Files.Add(filePath);

        return true;
    }

    public static void Dispose()
    {
        foreach (var file in Files)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                    Console.WriteLine($"deleted {file}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Something went wrong deleting {file}");
                Console.WriteLine(e);
            }
        }
    }

}
