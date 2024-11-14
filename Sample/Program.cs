// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;
using Interop.HostFxr;

class Program
{
    public delegate void CallMeDelegate();

    public static unsafe void Main()
    {
        var assemblyName      = typeof(Program).Assembly.GetName();
        var runtimeConfig     = $"{assemblyName}.runtimeconfig.json";
        var runtimeConfigPath = Path.Combine(AppContext.BaseDirectory, runtimeConfig);

        Console.WriteLine("Loading hostfxr..");

        var hostfxr = new HostFxr();

        hostfxr.InitializeForRuntimeConfig(runtimeConfigPath);

        delegate* unmanaged[Stdcall]<void> fpCallMe = null;

        hostfxr.LoadAssemblyAndGetFunctionPointer(
            typeof(Program).Assembly.Location,
            typeof(Program).AssemblyQualifiedName!,
            "Test",
            HostFxr.UNMANAGEDCALLERSONLY_METHOD,
            null,
            (void**)&fpCallMe
        );

        Console.WriteLine($"Calling the function at 0x{(ulong)fpCallMe:X}");

        /*

        Or, if you want to use the delegate type:

        hostfxr.LoadAssemblyAndGetFunctionPointer(
            typeof(Program).Assembly.Location,
            typeof(Program).AssemblyQualifiedName!,
            "Test",
            "Sample.Program+CallMeDelegate, Sample",
            null,
            (void**)&fpCallMe
        );

        */
    }


    [UnmanagedCallersOnly]
    public static void CallMe()
    {
        Console.WriteLine("You called me!");
    }
}
