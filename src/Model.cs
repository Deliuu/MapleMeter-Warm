using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace MapleMeter {
 public sealed class Session {
  public bool RequireFreshLevel;
  public bool Running,Paused,AutoPaused,Incomplete,Estimated;
  public Reading Last; public long Gain,Loss; public double Seconds,LastAcceptedAt=-1;
  public int Valid,Attempts; public string Note="等待开始";
  public DateTime Started;
  public readonly List<DailyValue> Days=new List<DailyValue>();double startedTick;
  DailyValue Day(DateTime date){var d=Days.FirstOrDefault(x=>x.Date==date.Date);if(d==null){d=new DailyValue{Date=date.Date};Days.Add(d);}return d;}
  public string Id;public Reading First;
  readonly MinuteSeries minutes=new MinuteSeries();
  public List<MinuteData> Minutes {get{return minutes.Snapshot(Seconds,Running,LastAcceptedAt);}}
  readonly List<Tuple<double,long>> points=new List<Tuple<double,long>>();
  readonly Dictionary<int,Tuple<double,double>> bounds=new Dictionary<int,Tuple<double,double>>();
  public readonly Dictionary<int,long> Requirements=new Dictionary<int,long>();
  Reading pending; int confirmations; double lastTick; bool rebase;
  public void Start(double now) {
   Days.Clear();startedTick=now;Running=true;Paused=AutoPaused=false;Incomplete=false;Estimated=false;Last=null;Gain=Loss=0;Seconds=0;Valid=Attempts=0;
   pending=null;confirmations=0;points.Clear();bounds.Clear();minutes.Clear();First=null;Id=Guid.NewGuid().ToString("N");lastTick=now;LastAcceptedAt=-1;Started=DateTime.Now;rebase=false;Note="正在确认起始读数";
  }
  public void Tick(double now) { if(Running&&!Paused){double elapsed=Math.Max(0,now-lastTick);Seconds+=elapsed;var from=Started.AddSeconds(lastTick-startedTick);var until=from.AddSeconds(elapsed);while(from<until){var end=from.Date.AddDays(1);if(end>until)end=until;Day(from).Seconds+=(end-from).TotalSeconds;from=end;}}lastTick=now; }
  public void UpdateIdle(double now,double idleSeconds) {
   if(Running&&!Paused&&idleSeconds>=15) {
    Tick(Math.Max(lastTick,now-(idleSeconds-15)));Paused=AutoPaused=true;lastTick=now;
    pending=null;confirmations=0;Note="已暂停";
   } else Tick(now);
  }
  public void Pause(double now) {Tick(now);Paused=true;AutoPaused=false;Note="已暂停";}
  public void Resume(double now) {lastTick=now;Paused=AutoPaused=false;rebase=true;pending=null;confirmations=0;Note="识别中";}
  public void Disconnect(){rebase=true;pending=null;confirmations=0;Incomplete=true;points.Clear();Note="窗口断开 · 重连后建立新基线";}
  public double Rate(double window) {
   if(points.Count<2||LastAcceptedAt<0||Seconds-LastAcceptedAt>3) return double.NaN;
   double start=Math.Max(0,Seconds-window);
   var p=points.FirstOrDefault(x=>x.Item1>=start)??points[0];
   double dt=Seconds-p.Item1; return dt<5?double.NaN:(Gain-p.Item2)*3600/dt;
  }
  public double Average {get{return Seconds>=5?Gain*3600/Seconds:double.NaN;}}
  public double Coverage {get{return Attempts==0?0:100.0*Valid/Attempts;}}
  public double Requirement(int level) {
   long exact; if(Requirements.TryGetValue(level,out exact)) return exact;
   Tuple<double,double> b;return bounds.TryGetValue(level,out b)?(b.Item1+b.Item2)/2:double.NaN;
  }
  void Learn(Reading r) {
   if(r.Percent<.1||r.Exp==0) return;
   // Allow either rounded or truncated two-decimal UI percentages; this is an estimate, not a verified table.
   double lo=r.Exp*100/(r.Percent+.011),hi=r.Exp*100/Math.Max(.001,r.Percent-.006);
   Tuple<double,double> b;
   if(bounds.TryGetValue(r.Level,out b)) {lo=Math.Max(lo,b.Item1);hi=Math.Min(hi,b.Item2);}
   if(lo<=hi) bounds[r.Level]=Tuple.Create(lo,hi);
  }
  public bool Accept(Reading r) {
   if(!Running||(Paused&&!AutoPaused)) return false;
   Attempts++;
   if(r==null) {Note="画面不可读 · 等待可靠数据";return false;}
   if(r.Confidence<25) {Note="数字不清晰 · 等待确认";return false;}
   if(RequireFreshLevel&&Last!=null&&r.Exp<Last.Exp&&!r.LevelRechecked){pending=null;confirmations=0;Note="等待重新确认等级";return false;}
   double req=Requirement(r.Level);
   if(!double.IsNaN(req) && Math.Abs(r.Exp/req*100-r.Percent)>(Requirements.ContainsKey(r.Level)?.016:.12)) {Note="经验与百分比不一致 · 已拦截";return false;}
   if(AutoPaused&&Last!=null&&Last.Level==r.Level&&Last.Exp==r.Exp){pending=null;confirmations=0;return false;}
   // Every new sample needs a second identical or coherent successor. A one-frame spike never commits.
   bool coherent=pending!=null && pending.Level==r.Level && r.Exp>=pending.Exp && r.Percent>=pending.Percent &&
    (r.Exp==pending.Exp || (r.Percent>pending.Percent && Math.Abs((r.Exp-pending.Exp)/Math.Max(1.0,r.Exp)*100)<20));
   if(!coherent) {pending=r;confirmations=1;Note="校验读数中";return false;}
   confirmations++; if(confirmations<2) return false;
   pending=r;
   if(AutoPaused) {
    if(Last!=null&&Last.Level==r.Level&&Last.Exp==r.Exp)return false;
    Paused=AutoPaused=false;Note="记录中";
   }
   if(Last==null||rebase) {if(First==null)First=r;if(rebase)minutes.MarkGap(Math.Max(0,LastAcceptedAt),Seconds);Last=r;Learn(r);rebase=false;Valid++;LastAcceptedAt=Seconds;points.Add(Tuple.Create(Seconds,Gain));Note="识别正常";return true;}
   long delta=0;
   if(r.Level==Last.Level) delta=r.Exp-Last.Exp;
   else if(r.Level>Last.Level&&r.Level-Last.Level<=5) {
    double total=r.Exp-Last.Exp;bool missing=false;
    for(int lv=Last.Level;lv<r.Level;lv++) {double need=Requirement(lv);if(double.IsNaN(need)) {missing=true;break;}total+=need;if(!Requirements.ContainsKey(lv)) Estimated=true;}
    if(missing||total<0) {Incomplete=true;Note="跨级经验表缺失 · 已分段";points.Clear();}
    else {delta=(long)Math.Round(total);Note=Estimated?"升级衔接 · 跨级累计为估算":"升级已衔接";}
   } else {Incomplete=true;Note="等级变化异常 · 已分段";points.Clear();}
   if(delta<0) {Loss-=delta;Note="已记录经验减少";}
   else if(r.Level==Last.Level) Note="识别正常";
   if(Seconds-LastAcceptedAt>5)minutes.MarkGap(Math.Max(0,LastAcceptedAt),Seconds);
   minutes.Add(Seconds,delta,r.Level!=Last.Level&&!Requirements.ContainsKey(Last.Level));
   var daily=Day(Started.AddSeconds(lastTick-startedTick));daily.Gain+=delta;if(delta<0)daily.Loss-=delta;daily.Approximate|=Estimated||Incomplete;Gain+=delta;Last=r;Learn(r);Valid++;LastAcceptedAt=Seconds;
   points.Add(Tuple.Create(Seconds,Gain));while(points.Count>0&&points[0].Item1<Seconds-3600)points.RemoveAt(0);
   return true;
  }
  public List<Tuple<double,long>> Points {get{return points.ToList();}}
 }
 public static partial class Storage {
  public static readonly string Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MapleMeter-Warm");
  public static string History {get{return Path.Combine(Folder,"sessions.csv");}}
  public static bool Save(Session s,string label){return SaveTo(Folder,s,label);}
 }
}
