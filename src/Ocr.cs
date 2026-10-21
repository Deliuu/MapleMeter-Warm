using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace MapleMeter {
 public sealed class Reading {
  public int Level; public long Exp; public double Percent; public double Confidence;
  public string Raw;public bool LevelRechecked;
 }
 public sealed class Ocr : IDisposable {
  const string Dll="libtesseract-5.dll";
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern bool SetDllDirectory(string path);
  [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern IntPtr TessBaseAPICreate();
  [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern int TessBaseAPIInit3(IntPtr a,string path,string lang);
  [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern void TessBaseAPISetPageSegMode(IntPtr a,int mode);
  [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern int TessBaseAPISetVariable(IntPtr a,string name,string val);
  [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern void TessBaseAPISetImage(IntPtr a,IntPtr bytes,int w,int h,int bpp,int stride);
  [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern IntPtr TessBaseAPIGetUTF8Text(IntPtr a);
  [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern IntPtr TessBaseAPIGetTsvText(IntPtr a,int page);
  [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern int TessBaseAPIMeanTextConf(IntPtr a);
  [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern void TessDeleteText(IntPtr p);
  [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern void TessBaseAPIDelete(IntPtr a);
  public string LastAttempt;
  IntPtr api; PixelFont pixels; long lastLevelHash; string lastLevelText;double lastLevelConfidence;
  public Ocr(string runtime) {
   Environment.SetEnvironmentVariable("OMP_THREAD_LIMIT","2");
   SetDllDirectory(runtime); api=TessBaseAPICreate();
   if(TessBaseAPIInit3(api,Path.Combine(runtime,"tessdata"),"eng")!=0) throw new Exception("无法加载本地 OCR 模型");
   TessBaseAPISetVariable(api,"user_defined_dpi","144");
   TessBaseAPISetVariable(api,"debug_file","NUL");
   pixels=new PixelFont(Path.Combine(Path.GetDirectoryName(runtime),"templates"));
  }
  public string Text(Bitmap input,int mode,bool digits,out double conf) {
   TessBaseAPISetPageSegMode(api,mode);
   TessBaseAPISetVariable(api,"tessedit_char_whitelist",digits?"0123456789.,[]()%/":"");
   var data=input.LockBits(new Rectangle(0,0,input.Width,input.Height),ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);
   try {
    TessBaseAPISetImage(api,data.Scan0,input.Width,input.Height,3,data.Stride);
    IntPtr p=TessBaseAPIGetUTF8Text(api);
    try { conf=TessBaseAPIMeanTextConf(api); return Marshal.PtrToStringAnsi(p)??""; }
    finally { TessDeleteText(p); }
   } finally { input.UnlockBits(data); }
  }
  public static Bitmap Prepare(Bitmap source,bool threshold,int cutoff=145) {
   int scale=source.Height<60?3:2;
   var output=new Bitmap(source.Width*scale,source.Height*scale,PixelFormat.Format24bppRgb);
   using(var g=Graphics.FromImage(output)) {
    g.Clear(Color.White); g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
    g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.Half;
    g.DrawImage(source,new Rectangle(0,0,output.Width,output.Height));
   }
   if(threshold) {
    var d=output.LockBits(new Rectangle(0,0,output.Width,output.Height),ImageLockMode.ReadWrite,PixelFormat.Format24bppRgb);
    byte[] bytes=new byte[d.Stride*d.Height]; Marshal.Copy(d.Scan0,bytes,0,bytes.Length);
    for(int y=0;y<d.Height;y++) for(int x=0;x<d.Width;x++) {
     int i=y*d.Stride+x*3; byte v=(byte)(Math.Min(bytes[i],Math.Min(bytes[i+1],bytes[i+2]))>cutoff?0:255);
     bytes[i]=bytes[i+1]=bytes[i+2]=v;
    }
    Marshal.Copy(bytes,0,d.Scan0,bytes.Length); output.UnlockBits(d);
   }
   return output;
  }
  public Reading Read(Bitmap exp,Bitmap level,Reading previous=null) {
   double ec,lc;string et=pixels.ReadExp(exp,out ec),lt;
   Reading value=ParseScreen(et,"1",ec);
   if(value==null){using(var b=Prepare(exp,true))et=Text(b,7,false,out ec);value=ParseScreen(et,"1",ec);}
   if(value==null){using(var b=Prepare(exp,false))et=Text(b,7,false,out ec);value=ParseScreen(et,"1",ec);}
   if(value==null||ec<25){LastAttempt="EXP unreadable: "+et;return null;}
   bool force=previous!=null&&(value.Exp<previous.Exp||value.Percent<previous.Percent);long hash=Hash(level);bool fresh=false;
   if(!force&&lastLevelText!=null&&lastLevelConfidence>=25&&hash==lastLevelHash){lt=lastLevelText;lc=lastLevelConfidence;}
   else {fresh=true;lt=pixels.ReadLevel(level,out lc);if(string.IsNullOrWhiteSpace(lt)){using(var b=Prepare(level,true))lt=Text(b,13,true,out lc);}
    if(string.IsNullOrWhiteSpace(lt)||lc<25){lastLevelText=null;LastAttempt="Level unreadable: "+lt+" confidence="+lc;return null;}
    lastLevelText=lt;lastLevelConfidence=lc;lastLevelHash=hash;
   }
   var result=ParseScreen(et,lt,Math.Min(ec,lc));if(result!=null)result.LevelRechecked=fresh;LastAttempt=et+" | "+lt;return result;
  }
  static long Hash(Bitmap b){unchecked{long h=1469598103934665603;for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++)h=(h^b.GetPixel(x,y).ToArgb())*1099511628211;return h;}}
  public MapReading ReadMap(Bitmap image,out Rectangle region) {
   region=pixels.LocateMap(image);if(region.IsEmpty)return null;
   using(var crop=image.Clone(region,PixelFormat.Format24bppRgb))return ReadMapTitle(crop);
  }
  public MapReading ReadMapTitle(Bitmap crop) {
   // Read each visible title line separately. Block OCR can discard an isolated
   // suffix number as a separate paragraph; keep every component through the right edge.
   using(var prepared=Prepare(crop,true,180)){
    var bands=new List<Rectangle>();int start=-1;
    for(int y=0;y<=prepared.Height;y++){bool ink=false;if(y<prepared.Height)for(int x=0;x<prepared.Width;x++)if(prepared.GetPixel(x,y).R<128){ink=true;break;}if(ink&&start<0)start=y;if(!ink&&start>=0){if(y-start>=3)bands.Add(new Rectangle(0,start,prepared.Width,y-start));start=-1;}}
    if(bands.Count==2){var lines=new List<string>();double confidence=100;foreach(var band in bands){using(var line=new Bitmap(band.Width+24,band.Height+24,PixelFormat.Format24bppRgb)){using(var g=Graphics.FromImage(line)){g.Clear(Color.White);g.DrawImage(prepared,new Rectangle(12,12,band.Width,band.Height),band,GraphicsUnit.Pixel);}double c;lines.Add(Text(line,7,false,out c).Trim());confidence=Math.Min(confidence,c);}}var result=MapReading.Parse(string.Join("\n",lines),confidence);if(result!=null)return result;}
    double fallback;return MapReading.Parse(Text(prepared,6,false,out fallback),fallback);
   }
  }
  public string Tsv(Bitmap input) {
   double c; Text(input,11,false,out c);
   IntPtr p=TessBaseAPIGetTsvText(api,0);
   try {return Marshal.PtrToStringAnsi(p)??"";} finally {TessDeleteText(p);}
  }
  public bool Locate(Bitmap strip,out Rectangle exp,out Rectangle level) {
   if(pixels.Locate(strip,out exp,out level))return true;
   exp=level=Rectangle.Empty;
   using(var b=Prepare(strip,false)) {
    string tsv=Tsv(b); var words=new List<Tuple<Rectangle,string>>();
    foreach(string line in tsv.Split('\n')) {
     string[] cols=line.Split('\t'); int x,y,w,h;
     if(cols.Length<12||cols[0]!="5"||!int.TryParse(cols[6],out x)||!int.TryParse(cols[7],out y)||!int.TryParse(cols[8],out w)||!int.TryParse(cols[9],out h)) continue;
     words.Add(Tuple.Create(new Rectangle(x,y,w,h),cols[11].Trim()));
    }
    foreach(var word in words) {
     if(Regex.IsMatch(word.Item2,@"^EXP[.:]?",RegexOptions.IgnoreCase)) {
      Rectangle r=word.Item1;
      foreach(var n in words) if(n.Item1.Left>=r.Left && n.Item1.Left<word.Item1.Left+word.Item1.Height*17 && Math.Abs(n.Item1.Top-word.Item1.Top)<word.Item1.Height*.6 && Regex.IsMatch(n.Item2,@"[\d%]")) r=Rectangle.Union(r,n.Item1);
      r.Inflate(5,5); exp=r;
     }
     if(Regex.IsMatch(word.Item2,@"^L[VY][.:]?",RegexOptions.IgnoreCase)) {
      Rectangle r=word.Item1;
      foreach(var n in words) if(n.Item1.Left>=r.Right && n.Item1.Left<word.Item1.Right+word.Item1.Height*4 && Math.Abs(n.Item1.Top-word.Item1.Top)<word.Item1.Height) {r=Rectangle.Union(r,n.Item1);break;}
      r.Inflate(5,5); level=r;
     }
    }
    double scale=(double)b.Width/strip.Width;
    exp=Unscale(exp,scale,strip.Size);level=Unscale(level,scale,strip.Size);
    return exp.Width>20 && level.Width>10;
   }
  }
  static Rectangle Unscale(Rectangle r,double scale,Size size) {
   if(r.IsEmpty) return r;
   return Rectangle.Intersect(new Rectangle(0,0,size.Width,size.Height),new Rectangle((int)(r.X/scale),(int)(r.Y/scale),(int)Math.Ceiling(r.Width/scale),(int)Math.Ceiling(r.Height/scale)));
  }
  public static Reading ParseScreen(string exp,string level,double confidence){
   // Without a visible separator, 9391.28% has multiple valid interpretations.
   // Retry OCR instead of silently assigning the final EXP digit to the percent.
   if(string.IsNullOrWhiteSpace(exp)||!Regex.IsMatch(exp,@"\d(?:\s*[\[(]\s*|\s+)\d{1,2}\.\d{2}\s*%"))return null;
   return Parse(exp,level,confidence);
  }
  public static Reading Parse(string exp,string level,double confidence) {
   var lm=Regex.Match(level,@"(?:LV[.\s:]*)?(\d{1,3})(?!\d)",RegexOptions.IgnoreCase);
   // A visible decimal percent is mandatory; never mistake HP or an isolated integer for EXP.
   var em=Regex.Match(exp.Trim(),@"^(?:EXP[.:\s]*)?(?<exp>\d[\d,\s]*?)\s*[\[\(]?\s*(?<pct>\d{1,2}\.\d{2})\s*%\s*[\]\)]?$",RegexOptions.IgnoreCase);
   int lv; long xp; double pct;
   if(!lm.Success||!em.Success||!int.TryParse(lm.Groups[1].Value,out lv)||lv<1||lv>300||
      !long.TryParse(Regex.Replace(em.Groups["exp"].Value,@"[^\d]",""),out xp)||
      !double.TryParse(em.Groups["pct"].Value,NumberStyles.Any,CultureInfo.InvariantCulture,out pct)||pct<0||pct>=100) return null;
   return new Reading{Level=lv,Exp=xp,Percent=pct,Confidence=confidence,Raw=exp.Trim()+" | "+level.Trim()};
  }
  public void Dispose() { if(api!=IntPtr.Zero) {TessBaseAPIDelete(api);api=IntPtr.Zero;} }
 }
}
