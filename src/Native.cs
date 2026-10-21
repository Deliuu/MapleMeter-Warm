using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace MapleMeter {
 public sealed class GameWindow {
  public IntPtr Handle; public string Title; public Rectangle Bounds;
  public override string ToString() { return Title + "  ·  " + Bounds.Width + " × " + Bounds.Height; }
 }
 public static class Native {
  public delegate bool EnumProc(IntPtr h, IntPtr p);
  [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc p, IntPtr x);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h,StringBuilder s,int n);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr h,out RECT r);
  [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr h,ref POINT p);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [StructLayout(LayoutKind.Sequential)] struct LASTINPUTINFO {public uint Size,Time;}
  [DllImport("user32.dll")] static extern bool GetLastInputInfo(ref LASTINPUTINFO info);
  // Only the timestamp is read. No keyboard hooks, key values, or mouse positions.
  public static double InputIdleSeconds() {var info=new LASTINPUTINFO{Size=8};return GetLastInputInfo(ref info)?unchecked((uint)Environment.TickCount-info.Time)/1000.0:0;}
  [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr h,int index);
  [DllImport("user32.dll")] public static extern int SetWindowLong(IntPtr h,int index,int value);
  [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr h,int id,uint mod,uint key);
  [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h,int id);
  [DllImport("user32.dll")] public static extern bool SetWindowDisplayAffinity(IntPtr h,uint affinity);
  [StructLayout(LayoutKind.Sequential)] struct RECT { public int L,T,R,B; }
  [StructLayout(LayoutKind.Sequential)] struct POINT { public int X,Y; }
  public static Rectangle ClientBounds(IntPtr h) {
   RECT r; POINT p=new POINT(); if(!GetClientRect(h,out r)||!ClientToScreen(h,ref p)) return Rectangle.Empty;
   return new Rectangle(p.X,p.Y,r.R-r.L,r.B-r.T);
  }
  public static List<GameWindow> Windows(bool all) {
   var items=new List<GameWindow>();
   EnumWindows(delegate(IntPtr h,IntPtr p) {
    if(!IsWindowVisible(h)) return true;
    var b=new StringBuilder(512); GetWindowText(h,b,b.Capacity); string title=b.ToString();
    Rectangle rect=ClientBounds(h);
    if(title.Length>0 && !title.StartsWith("MapleMeter") && rect.Width>=500 && rect.Height>=300 &&
      (all || title.IndexOf("Maple",StringComparison.OrdinalIgnoreCase)>=0 || title.Contains("冒险岛") || title.Contains("楓之谷")))
       items.Add(new GameWindow {Handle=h,Title=title,Bounds=rect});
    return true;
   },IntPtr.Zero);
   return items;
  }
  [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h,out RECT r);
  [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr h,int attribute,out RECT r,int size);
  [DllImport("dwmapi.dll",EntryPoint="DwmGetWindowAttribute")] static extern int DwmGetCloaked(IntPtr h,int attribute,out int value,int size);
  public static bool RegionVisible(IntPtr target,Rectangle area){
   if(!IsWindowVisible(target)||IsIconic(target)||area.IsEmpty||!System.Windows.Forms.SystemInformation.VirtualScreen.Contains(area))return false;
   bool found=false,blocked=false;
   EnumWindows(delegate(IntPtr h,IntPtr unused){if(h==target){found=true;return false;}if(!IsWindowVisible(h)||IsIconic(h))return true;int cloaked;if(DwmGetCloaked(h,14,out cloaked,4)==0&&cloaked!=0)return true;RECT r;if(DwmGetWindowAttribute(h,9,out r,16)!=0&&!GetWindowRect(h,out r))return true;if(Rectangle.FromLTRB(r.L,r.T,r.R,r.B).IntersectsWith(area)){blocked=true;return false;}return true;},IntPtr.Zero);
   return found&&!blocked;
  }
  public static bool HudVisible(IntPtr h,Rectangle strip,Rectangle exp,Rectangle level){exp.Offset(strip.Location);level.Offset(strip.Location);return RegionVisible(h,exp)&&RegionVisible(h,level);}
  public static Bitmap Capture(Rectangle r) {
   if(r.Width<=0||r.Height<=0) throw new InvalidOperationException("窗口尺寸无效");
   var b=new Bitmap(r.Width,r.Height,System.Drawing.Imaging.PixelFormat.Format24bppRgb);
   try { using(var g=Graphics.FromImage(b)) g.CopyFromScreen(r.Location,Point.Empty,r.Size,CopyPixelOperation.SourceCopy); return b; }
   catch { b.Dispose(); throw; }
  }
 }
}
