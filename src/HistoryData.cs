using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace MapleMeter {
 public sealed class HistoryRecord {
  public string Id,Map;
  public DateTime Started,Ended;
  public double Seconds;
  public long Gain,Loss;
  public int StartLevel,EndLevel;
  public double StartPercent,EndPercent;
  public bool Estimated,Incomplete,Legacy;
  public List<MinuteData> Minutes=new List<MinuteData>();
  [XmlIgnore]public double Rate {get{return Seconds>0?Gain*3600.0/Seconds:0;}}
  [XmlIgnore]public string Quality {get{return Incomplete?"有缺失":Estimated?"≈ 估算":"完整";}}
 }
 public static partial class Storage {
  public const double MinimumSeconds=600;
  public static bool Eligible(Session s){return s.Seconds>MinimumSeconds&&s.First!=null&&s.Last!=null&&s.Valid>=2;}
  public static bool SaveTo(string folder,Session s,string map) {
   DailyStorage.Save(folder,s);if(!Eligible(s))return false;
   string dir=Path.Combine(folder,"history");Directory.CreateDirectory(dir);
   string path=Path.Combine(dir,s.Id+".xml");if(File.Exists(path))return true;
   var record=new HistoryRecord{Id=s.Id,Map=map,Started=s.Started,Ended=DateTime.Now,Seconds=s.Seconds,Gain=s.Gain,Loss=s.Loss,StartLevel=s.First.Level,EndLevel=s.Last.Level,StartPercent=s.First.Percent,EndPercent=s.Last.Percent,Estimated=s.Estimated,Incomplete=s.Incomplete,Minutes=s.Minutes};
   record.Minutes.RemoveAll(x=>x.Seconds==0&&x.Gain==0);
   string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
   try {
    using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {new XmlSerializer(typeof(HistoryRecord)).Serialize(stream,record);stream.Flush(true);}
    File.Move(temp,path);
   }finally{if(File.Exists(temp))File.Delete(temp);}
   return true;
  }
  public static List<HistoryRecord> Load(string folder,out int skipped) {
   skipped=0;var records=new List<HistoryRecord>();string dir=Path.Combine(folder,"history");
   if(Directory.Exists(dir))foreach(string path in Directory.GetFiles(dir,"*.xml"))try {
    using(var reader=XmlReader.Create(path,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null})) {
     var r=(HistoryRecord)new XmlSerializer(typeof(HistoryRecord)).Deserialize(reader);
     if(double.IsNaN(r.Seconds)||double.IsInfinity(r.Seconds)||r.Seconds<0)throw new InvalidDataException();
     if(r.Seconds>MinimumSeconds)records.Add(r);
    }
   }catch{skipped++;}
   string legacy=Path.Combine(folder,"sessions.csv");
   if(File.Exists(legacy)) {
    var rows=ParseCsv(File.ReadAllText(legacy,Encoding.UTF8));int index=0;
    foreach(var row in rows.Skip(1)){index++;if(row.All(string.IsNullOrWhiteSpace))continue;try {
     DateTime date;double seconds;long gain,loss;
     if(row.Count<9||!DateTime.TryParseExact(row[0],"yyyy-MM-dd HH:mm:ss",CultureInfo.InvariantCulture,DateTimeStyles.None,out date)||!double.TryParse(row[2],NumberStyles.Float,CultureInfo.InvariantCulture,out seconds)||double.IsNaN(seconds)||double.IsInfinity(seconds)||!long.TryParse(row[3],out gain)||!long.TryParse(row[4],out loss))throw new InvalidDataException();
     if(seconds>MinimumSeconds)records.Add(new HistoryRecord{Id="legacy-"+index,Map=row[1],Started=date,Seconds=seconds,Gain=gain,Loss=loss,Estimated=row[7]=="是",Incomplete=row[8]=="是",Legacy=true});
    }catch{skipped++;}}
   }
   return records.OrderByDescending(r=>r.Started).ToList();
  }
  public static List<List<string>> ParseCsv(string text) {
   var rows=new List<List<string>>();var row=new List<string>();var cell=new StringBuilder();bool quoted=false;
   for(int i=0;i<text.Length;i++){
    char c=text[i];if(c=='"'){if(quoted&&i+1<text.Length&&text[i+1]=='"'){cell.Append('"');i++;}else quoted=!quoted;}
    else if(c==','&&!quoted){row.Add(cell.ToString());cell.Clear();}
    else if((c=='\n'||c=='\r')&&!quoted){if(c=='\r'&&i+1<text.Length&&text[i+1]=='\n')i++;row.Add(cell.ToString());cell.Clear();rows.Add(row);row=new List<string>();}
    else cell.Append(c);
   }
   if(cell.Length>0||row.Count>0){row.Add(cell.ToString());rows.Add(row);}
   return rows;
  }
  static string Cell(string text){text=text??"";if(text.Length>0&&"=+-@".IndexOf(text.TrimStart().FirstOrDefault())>=0)text="'"+text;return "\""+text.Replace("\"","\"\"")+"\"";}
  public static void Export(string path,IEnumerable<HistoryRecord> records) {
   var output=new StringBuilder("开始时间,地图,有效秒数,净经验,经验减少,平均每小时,开始等级,结束等级,状态\r\n");
   foreach(var r in records)output.AppendLine(string.Join(",",new[]{r.Started.ToString("yyyy-MM-dd HH:mm:ss"),Cell(r.Map),r.Seconds.ToString("R",CultureInfo.InvariantCulture),r.Gain.ToString(CultureInfo.InvariantCulture),r.Loss.ToString(CultureInfo.InvariantCulture),r.Rate.ToString("F0",CultureInfo.InvariantCulture),r.StartLevel==0?"":r.StartLevel.ToString(),r.EndLevel==0?"":r.EndLevel.ToString(),Cell(r.Quality)}));
   File.WriteAllText(path,output.ToString(),new UTF8Encoding(true));
  }
 }
}
