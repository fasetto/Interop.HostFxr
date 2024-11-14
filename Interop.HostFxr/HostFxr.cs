using System.Runtime.Versioning;
using System.Text;
using TerraFX.Interop.Windows;
using System.Runtime.InteropServices;
using PInvoke = TerraFX.Interop.Windows.Windows;

namespace Interop.HostFxr;

using unsafe HostFxrInitializeForRuntimeConfigFn = delegate* unmanaged[Cdecl]<
    char*,      // runtime_config_path
    void*,      // parameters
    void**,     // host_context_handle (output)
    int         // return type (status code)
>;

using unsafe HostFxrCloseFn = delegate* unmanaged[Cdecl]<
    void*,   // host_context_handle
    int      // return type
>;

using unsafe HostFxrGetRuntimeDelegateFn = delegate* unmanaged[Cdecl]<
    void*,                  // host_context_handle
    HostFxrDelegateType,    // delegate_type
    void**,                 // delegate (output pointer for function pointer)
    int                     // return type
>;

using unsafe LoadAssemblyAndGetFunctionPointerFn = delegate* unmanaged[Stdcall]<
    char*,    // assembly_path
    char*,    // type_name
    char*,    // method_name
    char*,    // delegate_type_name
    void*,    // reserved
    void**,   // delegate (output pointer for function pointer)
    int       // return type
>;

internal enum HostFxrDelegateType
{
    hdt_com_activation,
    hdt_load_in_memory_assembly,
    hdt_winrt_activation,
    hdt_com_register,
    hdt_com_unregister,
    hdt_load_assembly_and_get_function_pointer,
    hdt_get_function_pointer,
    hdt_load_assembly,
    hdt_load_assembly_bytes,
};


[SupportedOSPlatform("Windows")]
public unsafe class HostFxr
{
    private readonly string name = "hostfxr.dll";
    public static readonly int UNMANAGEDCALLERSONLY_METHOD = -1;

    private HostFxrCloseFn HostFxrClose;
    private LoadAssemblyAndGetFunctionPointerFn LoadAssemblyAndGetFunctionPointerFn;

    private HMODULE LoadLibrary(string path)
    {
        HMODULE hModule;

        fixed (char* lpPath = path)
        {
            hModule = PInvoke.LoadLibraryW(lpPath);
        }

        if (hModule == nint.Zero)
            throw new Exception($"Failed to load library: {path}");

        return hModule;
    }

    private void* GetProcAddress(HMODULE hModule, string procName)
    {
        var bytes = Encoding.Default.GetBytes(name);

        fixed (byte* lpProcName = bytes)
        {
            return PInvoke.GetProcAddress(hModule, (sbyte*)lpProcName);
        }
    }

    private void* InitializeForRuntimeConfig(HostFxrInitializeForRuntimeConfigFn lpInitRuntimeConfig, string runtimeConfigPath)
    {
        void* hostContextHandle = null;

        fixed (char* lpRuntimeConfigPath = runtimeConfigPath)
        {
            int status = lpInitRuntimeConfig(lpRuntimeConfigPath, null, &hostContextHandle);

            if (status is not (0 or 1))
            {
                HostFxrClose(hostContextHandle);
                throw new Exception($"Failed to initialize runtime config: {status}");
            }
        }

        return hostContextHandle;
    }

    public void InitializeForRuntimeConfig(string runtimeConfigPath)
    {
        HMODULE hostfxr = LoadLibrary(name);

        HostFxrClose = (HostFxrCloseFn)GetProcAddress(hostfxr, "hostfxr_close");

        var lpInitRuntimeConfig = (HostFxrInitializeForRuntimeConfigFn)GetProcAddress(hostfxr, "hostfxr_initialize_for_runtime_config");

        void* hHostContext = InitializeForRuntimeConfig(lpInitRuntimeConfig, runtimeConfigPath);

        var lpGetRuntimeDelegate = (HostFxrGetRuntimeDelegateFn)GetProcAddress(hostfxr, "hostfxr_get_runtime_delegate");

        void* lpLoadAssemblyAndGetFunctionPointer = NativeMemory.Alloc((nuint)sizeof(void*));

        var result = lpGetRuntimeDelegate(
            hHostContext,
            HostFxrDelegateType.hdt_load_assembly_and_get_function_pointer,
            &lpLoadAssemblyAndGetFunctionPointer
        );

        if (result is not 0)
        {
            HostFxrClose(hHostContext);
            throw new Exception($"Failed to get runtime delegate: {result}");
        }

        LoadAssemblyAndGetFunctionPointerFn = (LoadAssemblyAndGetFunctionPointerFn)lpLoadAssemblyAndGetFunctionPointer;

        HostFxrClose(hHostContext);
    }

    public void LoadAssemblyAndGetFunctionPointer(string assemblyLocation, string assemblyQualifiedName, string methodName, string delegateTypeName, void* reserved, void** fpDelegate)
    {
        fixed (char* lpAssemblyLocation = assemblyLocation)
        fixed (char* lpAssemblyQualifiedName = assemblyQualifiedName)
        fixed (char* lpMethodName = methodName)
        fixed (char* lpDelegateTypeName = delegateTypeName)
        {
            int result = this.LoadAssemblyAndGetFunctionPointerFn(
                lpAssemblyLocation,
                lpAssemblyQualifiedName,
                lpMethodName,
                lpDelegateTypeName,
                reserved,
                fpDelegate
            );

            if (result is not 0)
                throw new Exception($"Failed to load assembly and get function pointer: {result}");
        }
    }

    public void LoadAssemblyAndGetFunctionPointer(string assemblyLocation, string assemblyQualifiedName, string methodName, int delegateType, void* reserved, void** fpDelegate)
    {
        fixed (char* lpAssemblyLocation = assemblyLocation)
        fixed (char* lpAssemblyQualifiedName = assemblyQualifiedName)
        fixed (char* lpMethodName = methodName)
        {
            int result = this.LoadAssemblyAndGetFunctionPointerFn(
                lpAssemblyLocation,
                lpAssemblyQualifiedName,
                lpMethodName,
                (char*)delegateType,
                reserved,
                fpDelegate
            );

            if (result is not 0)
                throw new Exception($"Failed to load assembly and get function pointer: {result}");
        }
    }

}
