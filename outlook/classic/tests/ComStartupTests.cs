using System;
using System.Reflection;
using System.Runtime.InteropServices;
using EInvoiceViewer.OutlookClassic;

[ComVisible(true),ClassInterface(ClassInterfaceType.AutoDispatch)]
public sealed class FakeHost { public string Name {get {return "Synthetic host";}} }
internal static class ComStartupTests {
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int Connect(IntPtr self,IntPtr app,int mode,IntPtr addin,ref IntPtr custom);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int Disconnect(IntPtr self,int mode,ref IntPtr custom);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int Lifecycle(IntPtr self,ref IntPtr custom);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int Ribbon(IntPtr self,IntPtr id,out IntPtr markup);
    [StructLayout(LayoutKind.Sequential)] private struct DispatchParams {public IntPtr Args,NamedArgs;public uint Count,NamedCount;}
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int GetIds(IntPtr self,ref Guid iid,IntPtr names,uint count,uint locale,out int dispid);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int Invoke(IntPtr self,int dispid,ref Guid iid,uint locale,ushort flags,ref DispatchParams args,IntPtr result,IntPtr exception,IntPtr argError);
    [DllImport("oleaut32.dll")] private static extern int VariantClear(IntPtr variant);
    [DllImport("oleaut32.dll")] private static extern IntPtr SafeArrayCreateVector(ushort type,int lower,uint count);
    [DllImport("oleaut32.dll")] private static extern int SafeArrayDestroy(IntPtr array);
    private static T Slot<T>(IntPtr pointer,int index) where T:class {
        IntPtr vtable=Marshal.ReadIntPtr(pointer);
        return (T)(object)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(vtable,index*IntPtr.Size),typeof(T));
    }
    private static void Check(int hr) {Marshal.ThrowExceptionForHR(hr);}
    [STAThread] private static int Main() {
        try {
            foreach(AssemblyName reference in typeof(Addin).Assembly.GetReferencedAssemblies())
                if(reference.Name.Contains("WebView2") || reference.Name=="System.Security" || reference.Name=="System.Xml")
                    throw new Exception("Unexpected renderer dependency in Outlook bridge: "+reference.Name);
            Addin instance=new Addin();
            IntPtr unknown=Marshal.GetIUnknownForObject(instance),lifecycle=IntPtr.Zero,ribbon=IntPtr.Zero;
            IntPtr host=Marshal.GetIDispatchForObject(new FakeHost());
            try {
                Guid id=typeof(IDTExtensibility2).GUID;
                Check(Marshal.QueryInterface(unknown,ref id,out lifecycle));
                // Invoke the actual unmanaged COM vtable, including SAFEARRAY** marshaling.
                foreach(bool populated in new[]{false,true}) {
                    IntPtr custom=populated?SafeArrayCreateVector(12,0,0):IntPtr.Zero;
                    try {
                        Check(Slot<Connect>(lifecycle,7)(lifecycle,host,1,host,ref custom));
                        Check(Slot<Lifecycle>(lifecycle,9)(lifecycle,ref custom));
                        Check(Slot<Lifecycle>(lifecycle,10)(lifecycle,ref custom));
                        Check(Slot<Lifecycle>(lifecycle,11)(lifecycle,ref custom));
                        Check(Slot<Disconnect>(lifecycle,8)(lifecycle,0,ref custom));
                    } finally {if(custom!=IntPtr.Zero) Check(SafeArrayDestroy(custom));}
                }
                id=typeof(IRibbonExtensibility).GUID;
                Check(Marshal.QueryInterface(unknown,ref id,out ribbon));
                IntPtr text=Marshal.StringToBSTR("Microsoft.Outlook.Explorer"),markup=IntPtr.Zero;
                try {
                    Check(Slot<Ribbon>(ribbon,7)(ribbon,text,out markup));
                    if(!Marshal.PtrToStringBSTR(markup).Contains("OpenInvoice")) throw new Exception("Missing callback");
                } finally {Marshal.FreeBSTR(text);if(markup!=IntPtr.Zero) Marshal.FreeBSTR(markup);}
                IntPtr dispatch=Marshal.GetIDispatchForObject(instance);
                IntPtr name=Marshal.StringToCoTaskMemUni("GetAnySender"),names=Marshal.AllocCoTaskMem(IntPtr.Size);
                IntPtr argument=Marshal.AllocCoTaskMem(32),result=Marshal.AllocCoTaskMem(32);
                try {
                    Marshal.WriteIntPtr(names,name);
                    Guid empty=Guid.Empty;int dispid;
                    Check(Slot<GetIds>(dispatch,5)(dispatch,ref empty,names,1,0,out dispid));
                    if(dispid!=102) throw new Exception("Wrong default callback dispatch ID");
                    for(int i=0;i<32;i++) {Marshal.WriteByte(argument,i,0);Marshal.WriteByte(result,i,0);}
                    Marshal.WriteInt16(argument,9); // VT_DISPATCH with a null control pointer.
                    DispatchParams parameters=new DispatchParams {Args=argument,Count=1};
                    Check(Slot<Invoke>(dispatch,6)(dispatch,dispid,ref empty,0,1,ref parameters,result,IntPtr.Zero,IntPtr.Zero));
                    if(!(Marshal.GetObjectForNativeVariant(result) is bool)) throw new Exception("Invalid native ribbon callback result");
                    Marshal.FreeCoTaskMem(name);name=Marshal.StringToCoTaskMemUni("LoadRibbonImage");
                    Marshal.WriteIntPtr(names,name);
                    Check(Slot<GetIds>(dispatch,5)(dispatch,ref empty,names,1,0,out dispid));
                    if(dispid!=104) throw new Exception("Missing image callback dispatch ID");
                    IntPtr imageId=Marshal.StringToBSTR("InvoiceSearch");
                    try {
                        Marshal.WriteInt16(argument,8); // VT_BSTR image name.
                        Marshal.WriteIntPtr(argument,8,imageId);
                        Check(VariantClear(result));
                        Check(Slot<Invoke>(dispatch,6)(dispatch,dispid,ref empty,0,1,ref parameters,result,IntPtr.Zero,IntPtr.Zero));
                        object picture=Marshal.GetObjectForNativeVariant(result);
                        if(picture==null) throw new Exception("Image resource did not load");
                        IntPtr pictureUnknown=Marshal.GetIUnknownForObject(picture),pictureInterface=IntPtr.Zero;
                        try {
                            Guid pictureId=new Guid("7BF80981-BF32-101A-8BBB-00AA00300CAB");
                            Check(Marshal.QueryInterface(pictureUnknown,ref pictureId,out pictureInterface));
                        } finally {
                            if(pictureInterface!=IntPtr.Zero) Marshal.Release(pictureInterface);
                            Marshal.Release(pictureUnknown);
                        }
                        Check(VariantClear(result));
                    } finally {Marshal.FreeBSTR(imageId);}
                } finally {
                    Marshal.Release(dispatch);Marshal.FreeCoTaskMem(name);Marshal.FreeCoTaskMem(names);
                    Marshal.FreeCoTaskMem(argument);Marshal.FreeCoTaskMem(result);
                }
                if(instance.GetType().GetCustomAttributes(typeof(ComDefaultInterfaceAttribute),false).Length!=1)
                    throw new Exception("Missing explicit default dispatch interface");
            } finally {
                if(ribbon!=IntPtr.Zero) Marshal.Release(ribbon);
                if(lifecycle!=IntPtr.Zero) Marshal.Release(lifecycle);
                Marshal.Release(host);Marshal.Release(unknown);
            }
            Console.WriteLine("Native COM lifecycle and ribbon passed: "+(IntPtr.Size==8?"x64":"x86"));
            return 0;
        } catch(Exception error) {Console.Error.WriteLine(error);return 1;}
    }
}
