
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

public static class BindingUnpacker
{

    public static bool Unpack()
    {

        bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        // Fathom currently only exists for Windows
        if (!IsWindows)
        {
            return false;
        }

        string filename = "Fathom.fathomDll.dll";
        string resname = $"Truncator.{filename}";

        string path = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory), filename);

        // skip extraction, if the file already exists
        if (File.Exists(path))
        {
            Console.WriteLine($"Fathom binding already exists");
            return true;
        }
        Debug.WriteLine("File not found, now try to extract it");

        // now to extracting the file
        var asm = Assembly.GetExecutingAssembly();
        Debug.WriteLine($"looking for {resname} in [{string.Join(", ", asm.GetManifestResourceNames())}]");
        using Stream stream = asm.GetManifestResourceStream(resname);

        if (stream == null)
        {
            Console.WriteLine("Fathom bindings not found in asm");
            return false;
        }

        string dllPath = Path.Combine(path, filename);

        using FileStream fs = new FileStream(dllPath, FileMode.Create, FileAccess.Write);
        stream.CopyTo(fs);

        NativeLibrary.Load(path);

        return true;
    }

}
