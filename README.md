# Interop.HostFxr

## Overview

This is a project demonstrates how to use the HostFxr library to load .NET assemblies and execute managed code. This can be useful for scenarios where you need to call .NET code from native code.

## Getting Started

First, you need to enable unsafe code in your project settings.

```xml
<AllowUnsafeBlocks>true</AllowUnsafeBlocks>
```

```cs
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

        // If the method is marked with UnmanagedCallersOnlyAttribute
        hostfxr.LoadAssemblyAndGetFunctionPointer(
            typeof(Program).Assembly.Location,
            typeof(Program).AssemblyQualifiedName!,
            "CallMe",
            HostFxr.UNMANAGEDCALLERSONLY_METHOD,
            null,
            (void**)&fpCallMe
        );

        Console.WriteLine($"Calling the function at 0x{(ulong)fpCallMe:X}");

        fpCallMe();

        /*

        Or, if you want to use the delegate type:

        hostfxr.LoadAssemblyAndGetFunctionPointer(
            typeof(Program).Assembly.Location,
            typeof(Program).AssemblyQualifiedName!,
            "CallMe",
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
```

## References
1. https://github.com/dotnet/runtime/blob/main/docs/design/features/native-hosting.md
2. https://learn.microsoft.com/en-us/dotnet/core/tutorials/netcore-hosting
