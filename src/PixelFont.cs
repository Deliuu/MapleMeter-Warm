using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MapleMeter {
 public sealed class PixelFont {
  sealed class Mask {public int W,H;public bool[] P;}
  readonly Dictionary<char,Mask> levelGlyphs=new Dictionary<char,Mask>();
  readonly Dictionary<char,Mask> glyphs=new Dictionary<char,Mask>();Mask expAnchor,lvAnchor,mapAnchor;
  public PixelFont(string path) {
   foreach(string file in Directory.GetFiles(path,"level-*.png"))using(var b=new Bitmap(file))levelGlyphs[Path.GetFileNameWithoutExtension(file)[6]]=GetMask(b,true);
   foreach(string file in Directory.GetFiles(path,"digit-*.png")) {
    string name=Path.GetFileNameWithoutExtension(file).Substring(6);char c=name=="dot"?'.':name=="comma"?',':name=="percent"?'%':name[0];
    using(var b=new Bitmap(file))glyphs[c]=GetMask(b,true);
   }
   using(var b=new Bitmap(Path.Combine(path,"anchor-exp.png")))expAnchor=GetMask(b,true);
   using(var b=new Bitmap(Path.Combine(path,"anchor-lv.png")))lvAnchor=GetMask(b,true);
   using(var b=new Bitmap(Path.Combine(path,"anchor-map.png")))mapAnchor=GetMask(b,true);
  }
  static Mask GetMask(Bitmap input,bool black) {
   using(var b=input.Clone(new Rectangle(Point.Empty,input.Size),PixelFormat.Format24bppRgb)) {
    var d=b.LockBits(new Rectangle(Point.Empty,b.Size),ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);
    byte[] bytes=new byte[d.Stride*d.Height];Marshal.Copy(d.Scan0,bytes,0,bytes.Length);b.UnlockBits(d);
    var m=new Mask{W=b.Width,H=b.Height,P=new bool[b.Width*b.Height]};
    for(int y=0;y<m.H;y++)for(int x=0;x<m.W;x++){int i=y*d.Stride+x*3;int min=Math.Min(bytes[i],Math.Min(bytes[i+1],bytes[i+2]));m.P[y*m.W+x]=black?min<128:min>145;}
    return m;
   }
  }
  static Mask Resize(Mask m,double scale){int w=Math.Max(1,(int)Math.Round(m.W*scale)),h=Math.Max(1,(int)Math.Round(m.H*scale));var n=new Mask{W=w,H=h,P=new bool[w*h]};for(int y=0;y<h;y++)for(int x=0;x<w;x++)n.P[y*w+x]=m.P[Math.Min(m.H-1,(int)(y/scale))*m.W+Math.Min(m.W-1,(int)(x/scale))];return n;}
  static Rectangle Find(Mask image,Mask template,out double scaleFound) {
   double best=.86;Rectangle result=Rectangle.Empty;scaleFound=1;
   foreach(double scale in new[]{1.0,.5,.75,1.25,1.5,2.0}) {
    var t=Resize(template,scale);var probes=new List<int>();var emptyProbes=new List<int>();
    for(int i=0;i<t.P.Length;i++)if(t.P[i]&&(i%3==0))probes.Add(i);
    for(int i=0;i<t.P.Length;i++)if(!t.P[i]&&(i%3==0))emptyProbes.Add(i);
    if(probes.Count<4)continue;
    for(int y=0;y<=image.H-t.H;y++)for(int x=0;x<=image.W-t.W;x++) {
     bool candidate=true;for(int k=0;k<Math.Min(8,probes.Count);k++){int j=probes[k*(probes.Count-1)/Math.Max(1,Math.Min(8,probes.Count)-1)];if(!image.P[(y+j/t.W)*image.W+x+j%t.W]){candidate=false;break;}}
     if(!candidate)continue;
     for(int k=0;k<Math.Min(6,emptyProbes.Count);k++){int j=emptyProbes[k*(emptyProbes.Count-1)/Math.Max(1,Math.Min(6,emptyProbes.Count)-1)];if(image.P[(y+j/t.W)*image.W+x+j%t.W]){candidate=false;break;}}
     if(!candidate)continue;
     int match=0;for(int yy=0;yy<t.H;yy++)for(int xx=0;xx<t.W;xx++)if(image.P[(y+yy)*image.W+x+xx]==t.P[yy*t.W+xx])match++;
     double score=(double)match/t.P.Length;if(score>best){best=score;result=new Rectangle(x,y,t.W,t.H);scaleFound=scale;if(score>.995)return result;}
    }
   }
   return result;
  }
  public bool Locate(Bitmap strip,out Rectangle exp,out Rectangle level) {
   var m=GetMask(strip,false);double es,ls;var e=Find(m,expAnchor,out es);var l=Find(m,lvAnchor,out ls);
   exp=level=Rectangle.Empty;if(e.IsEmpty||l.IsEmpty)return false;
   exp=Rectangle.Intersect(new Rectangle(Point.Empty,strip.Size),new Rectangle(e.X+(int)(48*es),e.Y+(int)(3*es),(int)(178*es),(int)(20*es)));
   level=Rectangle.Intersect(new Rectangle(Point.Empty,strip.Size),new Rectangle(l.Right+(int)(19*ls),l.Y-(int)(5*ls),(int)(87*ls),(int)(37*ls)));
   return exp.Width>10&&level.Width>5;
  }
  public Rectangle LocateMap(Bitmap image) {
   double scale;var anchor=Find(GetMask(image,true),mapAnchor,out scale);
   if(anchor.IsEmpty)return Rectangle.Empty;
   return Rectangle.Intersect(new Rectangle(Point.Empty,image.Size),new Rectangle(anchor.X+(int)(85*scale),anchor.Y+(int)(30*scale),(int)(335*scale),(int)(74*scale)));
  }
  public string ReadExp(Bitmap input,out double confidence){
   // The green brackets are the HUD's explicit field separator. Digit 1 has
   // wide side bearings, so whitespace between white glyphs is not a delimiter.
   var brackets=new List<Rectangle>();int start=-1;
   for(int x=0;x<=input.Width;x++){int count=0;if(x<input.Width)for(int y=0;y<input.Height;y++){var c=input.GetPixel(x,y);if(c.G>c.R+20&&c.G>c.B+20&&c.G>100)count++;}bool ink=count>=Math.Max(2,input.Height*.3);if(ink&&start<0)start=x;if(!ink&&start>=0){brackets.Add(new Rectangle(start,0,x-start,input.Height));start=-1;}}
   if(brackets.Count==2&&brackets[0].Left>0&&brackets[1].Left>brackets[0].Right){using(var xp=input.Clone(new Rectangle(0,0,brackets[0].Left,input.Height),PixelFormat.Format24bppRgb))using(var pct=input.Clone(new Rectangle(brackets[0].Right,0,brackets[1].Left-brackets[0].Right,input.Height),PixelFormat.Format24bppRgb)){double a,b;string first=Read(xp,out a),second=Read(pct,out b);confidence=Math.Min(a,b);return first==null||second==null?null:first+" ["+second+"]";}}
   return Read(input,out confidence);
  }
  public string Read(Bitmap input,out double confidence){return ReadGlyphs(input,glyphs,.83,out confidence);}
  public string ReadLevel(Bitmap input,out double confidence){return ReadGlyphs(input,levelGlyphs,.96,out confidence);}
  string ReadGlyphs(Bitmap input,Dictionary<char,Mask> alphabet,double minimum,out double confidence) {
   var m=GetMask(input,false);var runs=new List<Rectangle>();int start=-1;
   for(int x=0;x<=m.W;x++) {
    bool ink=false;if(x<m.W)for(int y=0;y<m.H;y++)if(m.P[y*m.W+x]){ink=true;break;}
    if(ink&&start<0)start=x;
    if(!ink&&start>=0){int top=m.H,bottom=0;for(int xx=start;xx<x;xx++)for(int y=0;y<m.H;y++)if(m.P[y*m.W+xx]){top=Math.Min(top,y);bottom=Math.Max(bottom,y+1);}runs.Add(new Rectangle(start,top,x-start,bottom-top));start=-1;}
   }
   var output=new StringBuilder();double worst=1;
   foreach(var r in runs) {
    if(r.Height<1)continue;double best=0,second=0;char chosen='?';
    foreach(var pair in alphabet) {
     var t=pair.Value;double aspect=(double)r.Width/r.Height,ta=(double)t.W/t.H;
     if(Math.Abs(aspect-ta)>.35)continue;
     int diff=0,total=0;
     for(int y=0;y<28;y++)for(int x=0;x<20;x++){bool a=m.P[(r.Y+y*r.Height/28)*m.W+r.X+x*r.Width/20],b=t.P[(y*t.H/28)*t.W+x*t.W/20];if(a!=b)diff++;total++;}
     double score=1.0-(double)diff/total;if(score>best){second=best;best=score;chosen=pair.Key;}else second=Math.Max(second,score);
    }
    if(best<minimum||best-second<.025){confidence=0;return null;}
    worst=Math.Min(worst,best);output.Append(chosen);
   }
   confidence=worst*100;return output.ToString();
  }
 }
}
