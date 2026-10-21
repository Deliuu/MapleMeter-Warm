using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace MapleMeter {
 public sealed class MapReading {
  public string Region,Name;
  public string Key {get{return Region+" / "+Name;}}
  public static MapReading Parse(string text,double confidence) {
   if(confidence<55)return null;
   var lines=text.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries).Select(x=>Regex.Replace(x.Trim(),@"\s+"," ")).Where(x=>x.Length>0).ToArray();
   if(lines.Length!=2||lines.Any(x=>x.Length<3||x.Length>70||!Regex.IsMatch(x,@"^[A-Za-z0-9 '\-:.,()]+$")||x.Count(char.IsLetter)<3))return null;
   return new MapReading{Region=lines[0],Name=lines[1]};
  }
 }
 public sealed class MapTracker {
  public MapReading Current;MapReading pending;int count,misses;
  public double LastSeen=-100;
  public bool Observe(MapReading reading,double now) {
   if(reading==null){pending=null;count=0;misses++;return false;}
   if(pending!=null&&pending.Key==reading.Key)count++;else{pending=reading;count=1;}
   if(count<2)return false;
   LastSeen=now;misses=0;
   if(Current!=null&&Current.Key==reading.Key)return false;
   Current=reading;return true;
  }
  public bool Fresh(double now){return Current!=null&&misses<3;}
  public void Reset(){Current=pending=null;count=misses=0;LastSeen=-100;}
 }
}
