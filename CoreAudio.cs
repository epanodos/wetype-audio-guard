using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace AudioProbe {
[ComImport,Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] class Enumerator {}
[ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IEnum {
 [PreserveSig]int EnumAudioEndpoints(int flow,uint mask,out ICollection devices);
 [PreserveSig]int GetDefaultAudioEndpoint(int flow,int role,out IDevice device);
 [PreserveSig]int GetDevice([MarshalAs(UnmanagedType.LPWStr)]string id,out IDevice device);
 [PreserveSig]int RegisterEndpointNotificationCallback(IntPtr p);
 [PreserveSig]int UnregisterEndpointNotificationCallback(IntPtr p);
}
[ComImport,Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface ICollection {
 [PreserveSig]int GetCount(out uint count); [PreserveSig]int Item(uint n,out IDevice device);
}
[ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IDevice {
 [PreserveSig]int Activate(ref Guid iid,uint context,IntPtr p,[MarshalAs(UnmanagedType.IUnknown)]out object obj);
 [PreserveSig]int OpenPropertyStore(uint access,out IntPtr store);
 [PreserveSig]int GetId([MarshalAs(UnmanagedType.LPWStr)]out string id);
 [PreserveSig]int GetState(out uint state);
}
[ComImport,Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IManager {
 [PreserveSig]int GetAudioSessionControl(IntPtr guid,uint flags,out IntPtr control);
 [PreserveSig]int GetSimpleAudioVolume(IntPtr guid,uint flags,out IntPtr volume);
 [PreserveSig]int GetSessionEnumerator(out ISessions sessions);
 [PreserveSig]int RegisterSessionNotification(IntPtr p);
 [PreserveSig]int UnregisterSessionNotification(IntPtr p);
 [PreserveSig]int RegisterDuckNotification([MarshalAs(UnmanagedType.LPWStr)]string id,IntPtr p);
 [PreserveSig]int UnregisterDuckNotification(IntPtr p);
}
[ComImport,Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface ISessions {
 [PreserveSig]int GetCount(out int count); [PreserveSig]int GetSession(int n,out IControl control);
}
[ComImport,Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IControl {
 [PreserveSig]int GetState(out int state);
 [PreserveSig]int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)]out string name);
 [PreserveSig]int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)]string name,IntPtr ctx);
 [PreserveSig]int GetIconPath([MarshalAs(UnmanagedType.LPWStr)]out string path);
 [PreserveSig]int SetIconPath([MarshalAs(UnmanagedType.LPWStr)]string path,IntPtr ctx);
 [PreserveSig]int GetGroupingParam(out Guid id);
 [PreserveSig]int SetGroupingParam(ref Guid id,IntPtr ctx);
 [PreserveSig]int RegisterAudioSessionNotification(IntPtr p);
 [PreserveSig]int UnregisterAudioSessionNotification(IntPtr p);
 [PreserveSig]int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)]out string id);
 [PreserveSig]int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)]out string id);
 [PreserveSig]int GetProcessId(out uint pid);
 [PreserveSig]int IsSystemSoundsSession();
 [PreserveSig]int SetDuckingPreference([MarshalAs(UnmanagedType.Bool)]bool optout);
}
[ComImport,Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IVolume {
 [PreserveSig]int SetMasterVolume(float level,ref Guid ctx);
 [PreserveSig]int GetMasterVolume(out float level);
 [PreserveSig]int SetMute([MarshalAs(UnmanagedType.Bool)]bool mute,ref Guid ctx);
 [PreserveSig]int GetMute([MarshalAs(UnmanagedType.Bool)]out bool mute);
}
public class Session {
 public string Key; public string Device; public string Flow; public uint Pid; public string Process;
 public int State; public bool Mute; public float Volume;
}
public static class Core {
 static void Check(int hr) { Marshal.ThrowExceptionForHR(hr); }
 static void Release(object o) { if(o!=null && Marshal.IsComObject(o)) Marshal.ReleaseComObject(o); }
 public static Session[] Snapshot() { return Scan(null,false).ToArray(); }
 public static Session[] SetMute(string key,bool mute) { return Scan(key,mute).ToArray(); }
 static List<Session> Scan(string target,bool mute) {
 var result=new List<Session>(); var e=(IEnum)new Enumerator();
 try { for(int flow=0;flow<2;flow++) {
 ICollection devices=null; Check(e.EnumAudioEndpoints(flow,1,out devices));
 try {uint n; Check(devices.GetCount(out n)); for(uint d=0;d<n;d++) {
 IDevice dev=null; object managerObject=null; ISessions sessions=null;
 try {
 Check(devices.Item(d,out dev)); string deviceId; Check(dev.GetId(out deviceId));
 Guid iid=typeof(IManager).GUID; Check(dev.Activate(ref iid,23,IntPtr.Zero,out managerObject));
 var manager=(IManager)managerObject; Check(manager.GetSessionEnumerator(out sessions)); int count; Check(sessions.GetCount(out count));
 for(int i=0;i<count;i++) { IControl sc=null;
 try { Check(sessions.GetSession(i,out sc)); uint pid; int state; string key;
 Check(sc.GetProcessId(out pid)); Check(sc.GetState(out state)); Check(sc.GetSessionInstanceIdentifier(out key));
 key=deviceId+"|"+key; var v=(IVolume)sc; bool m; float volume;
 if(flow==0 && target==key) {var ctx=Guid.Empty; Check(v.SetMute(mute,ref ctx));}
 Check(v.GetMute(out m)); Check(v.GetMasterVolume(out volume));
 string name="System"; if(pid!=0) {try { using(var p=Process.GetProcessById((int)pid)) name=p.ProcessName; } catch { name="Exited"; }}
 result.Add(new Session{Key=key,Device=deviceId,Flow=flow==0?"Render":"Capture",Pid=pid,Process=name,State=state,Mute=m,Volume=volume});
 } finally {Release(sc);} }
 } finally {Release(sessions);Release(managerObject);Release(dev);} }
 } finally {Release(devices);} }
 } finally {Release(e);} return result;
 }
}
}
