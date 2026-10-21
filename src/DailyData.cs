using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
namespace MapleMeter {
 public sealed class PauseTimeout {
  double since=-1;
  public bool Observe(bool running,bool paused,double now){if(!running||!paused){since=-1;return false;}if(since<0)since=now;return now-since>=900;}
 }
 public sealed class DailyValue {public DateTime Date;public long Gain,Loss;public double Seconds;public bool Approximate;}
 public sealed class DailySession {public string Id;public List<DailyValue> Days=new List<DailyValue>();}
 public static class DailyStorage {
  public static void Save(string folder,Session session){if(session.First==null||session.Last==null||session.Valid<2)return;string dir=Path.Combine(folder,"daily");Directory.CreateDirectory(dir);string path=Path.Combine(dir,session.Id+".xml"),temp=path+".tmp";try{using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){new XmlSerializer(typeof(DailySession)).Serialize(stream,new DailySession{Id=session.Id,Days=session.Days});stream.Flush(true);}if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}
  public static List<DailyValue> Load(string folder,Session current,out int skipped){skipped=0;var sessions=new Dictionary<string,List<DailyValue>>();string dir=Path.Combine(folder,"daily");if(Directory.Exists(dir))foreach(var file in Directory.GetFiles(dir,"*.xml"))try{using(var reader=XmlReader.Create(file,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null})){var item=(DailySession)new XmlSerializer(typeof(DailySession)).Deserialize(reader);if(string.IsNullOrEmpty(item.Id)||item.Days.Any(x=>x.Seconds<0||double.IsNaN(x.Seconds)||double.IsInfinity(x.Seconds)))throw new InvalidDataException();sessions[item.Id]=item.Days;}}catch{skipped++;}
   int ignored;foreach(var r in Storage.Load(folder,out ignored))if(!sessions.ContainsKey(r.Id))sessions[r.Id]=new List<DailyValue>{new DailyValue{Date=r.Started.Date,Gain=r.Gain,Loss=r.Loss,Seconds=r.Seconds,Approximate=true}};skipped+=ignored;
   if(current!=null&&current.First!=null&&current.Valid>=2)sessions[current.Id]=current.Days;
   return Aggregate(sessions.Values.SelectMany(x=>x));
  }
  public static List<DailyValue> Aggregate(IEnumerable<DailyValue> values){return values.GroupBy(x=>x.Date.Date).Select(g=>new DailyValue{Date=g.Key,Gain=g.Sum(x=>x.Gain),Loss=g.Sum(x=>x.Loss),Seconds=g.Sum(x=>x.Seconds),Approximate=g.Any(x=>x.Approximate)}).OrderByDescending(x=>x.Date).ToList();}
 }
}
