
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

public static class BindingHandler
{

    private static List<string> Files = new List<string>();

    public static bool UnpackContainedDll(string resourceName, string fileName)
    {
        // first, create a new unique directory to load the dll into
        // fathoms probe_root() is not threadsafe and crashes if called
        // multiple times in parallel

        string guid = Guid.NewGuid().ToString();
        string tempDir = Path.Combine(AppContext.BaseDirectory, guid);
        string filePath = Path.Combine(tempDir, fileName);

        Directory.CreateDirectory(tempDir);

        Debug.WriteLine($"App Base Dir: {AppContext.BaseDirectory}");
        Debug.WriteLine($"created temp Dir: {tempDir}");

        // next, extract the assembly from the compiled program
        // then paste it into the correct file

        var asm = Assembly.GetEntryAssembly();
        Debug.WriteLine($"looking for {resourceName} in [{string.Join(", ", asm.GetManifestResourceNames())}]");
        Stream stream = asm.GetManifestResourceStream(resourceName) ?? throw new FileNotFoundException($"file not found :(");

        // then, paste the assembly in an external file
        // simply create the file anew, as it does not exist yet

        using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            stream.CopyTo(fs);
            fs.Flush(true);
            Debug.WriteLine($"info string 'Fathom Dll at: {filePath}'");
        }
        
        // load the temp directory and the dll for later use

        NativeLibrary.Load(filePath);

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
                    Debug.WriteLine($"deleted {file}");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Something went wrong deleting {file}");
                Debug.WriteLine(e);
            }
        }
    }

}
