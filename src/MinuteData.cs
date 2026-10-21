using System;
using System.Collections.Generic;
using System.Linq;

namespace MapleMeter {
 public sealed class MinuteData {
  public int Index;public long Gain;public double Seconds;public bool Gap,Estimated;
  public MinuteData Copy(){return (MinuteData)MemberwiseClone();}
 }
 public sealed class MinuteSeries {
  readonly List<MinuteData> items=new List<MinuteData>();
  void Ensure(int index){while(items.Count<=index)items.Add(new MinuteData{Index=items.Count});}
  public void Clear(){items.Clear();}
  public void Add(double at,long gain,bool estimated){int i=(int)(at/60);Ensure(i);items[i].Gain+=gain;items[i].Estimated|=estimated;}
  public void MarkGap(double from,double to){int end=(int)(to/60);Ensure(end);for(int i=Math.Max(0,(int)(from/60));i<=end;i++)items[i].Gap=true;}
  public List<MinuteData> Snapshot(double seconds,bool running,double lastAccepted) {
   int end=(int)(seconds/60);Ensure(end);
   var copy=items.Take(end+1).Select(x=>x.Copy()).ToList();
   foreach(var b in copy){b.Seconds=Math.Min(60,Math.Max(0,seconds-b.Index*60));if(lastAccepted<0||seconds-lastAccepted>5){if((b.Index+1)*60>Math.Max(0,lastAccepted))b.Gap=true;}}
   if(!running&&copy.Count>0&&copy.Last().Seconds==0&&copy.Last().Gain==0)copy.RemoveAt(copy.Count-1);
   return copy;
  }
 }
}
