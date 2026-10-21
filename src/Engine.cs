using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
namespace MapleMeter {
 public sealed class CaptureResult:IDisposable {
  public Reading Reading;public Bitmap Strip;public Rectangle Exp,Level;public string Status;public double Ms;public bool MapSampled;public MapReading Map;
  public void Dispose(){if(Strip!=null)Strip.Dispose();}
 }
 public sealed class Engine:IDisposable {
  public readonly Session Session=new Session();readonly Stopwatch clock=Stopwatch.StartNew();readonly DispatcherTimer timer=new DispatcherTimer();
  readonly MapTracker maps=new MapTracker();readonly List<string> visited=new List<string>();
  public GameWindow Target{get;private set;}public Bitmap LatestStrip{get;private set;}public Rectangle ExpRect{get;private set;}public Rectangle LevelRect{get;private set;}
  Size calibratedSize;Ocr ocr;bool busy,disposed,autoStart=true,demo;int sourceVersion;int failedReads;bool suspended;
  readonly PauseTimeout pauseTimeout=new PauseTimeout();
  double lastGameActivity,lastMapScan=-10,lastFind=-10,lastLocate=-10,lastRead=-10,noticeUntil;
  string notice="";public string Diagnostic="等待游戏";public double Latency;public event Action Updated;
  public Engine(bool demonstration=false){demo=demonstration;Session.RequireFreshLevel=!demo;LoadTable();timer.Interval=TimeSpan.FromMilliseconds(250);timer.Tick+=async delegate{try{await Pulse();}catch(Exception ex){Diagnostic=ex.Message;Notify("识别暂不可用");}};if(demo)Demo();}
  public void Start(){if(!demo){RefreshWindows();timer.Start();}Raise();}
  void Raise(){if(Updated!=null)Updated();}
  public bool DemoMode{get{return demo;}}
  public string MapName{get{return maps.Fresh(clock.Elapsed.TotalSeconds)?maps.Current.Name:"等待地图";}}
  public string Region{get{return maps.Fresh(clock.Elapsed.TotalSeconds)?maps.Current.Region:"";}}
  string MapLabel{get{return visited.Count==0?"未识别地图":string.Join(" → ",visited);}}
  public bool Stale{get{return !demo&&clock.Elapsed.TotalSeconds-lastRead>3;}}
  public double Rate{get{return Session.Paused||Stale?double.NaN:Session.Rate(300);}}
  public string Status{get{if(demo)return "演示数据";if(clock.Elapsed.TotalSeconds<noticeUntil)return notice;if(Target==null)return "等待游戏";if(!Session.Running)return autoStart?"连接中":"已结束";if(Session.Paused)return "已暂停";return Stale?(Diagnostic=="状态栏被遮挡"||Diagnostic=="游戏已最小化"?Diagnostic:"等待识别"):"记录中";}}
  public string Eta{get{if(Session.Last==null)return "统计中";if(Session.Last.Level==100&&!Session.Requirements.ContainsKey(100))return "已满级";double need=Session.Requirement(Session.Last.Level),rate=Rate;if(double.IsNaN(need)||double.IsNaN(rate)||rate<=0)return "统计中";double m=Math.Max(0,need-Session.Last.Exp)/rate*60;if(m>6000)return "> 100 小时";return "约 "+(m>=60?((int)m/60)+"时 "+((int)m%60)+"分":Math.Ceiling(m)+" 分钟");}}
  public void Notify(string text){notice=text;noticeUntil=clock.Elapsed.TotalSeconds+4;Raise();}
  void RefreshWindows(){var found=Native.Windows(false);var exact=found.Where(x=>x.Title.Equals("MapleStory",StringComparison.OrdinalIgnoreCase)).ToList();if(exact.Count>0)found=exact;if(found.Count==1){Target=found[0];ResetLocation();}else if(found.Count>1)Diagnostic="多个游戏窗口，请在设置中选择";}
  public bool SelectWindow(GameWindow window){if(Session.Running)EndSession();Target=window;ResetLocation();maps.Reset();lastMapScan=-10;autoStart=true;Raise();return true;}
  public void ResetLocation(){if(LatestStrip!=null){LatestStrip.Dispose();LatestStrip=null;}sourceVersion++;ExpRect=LevelRect=Rectangle.Empty;lastRead=lastLocate=-10;failedReads=0;Diagnostic="等待游戏画面";}
  public void ApplyCalibration(Rectangle exp,Rectangle level){sourceVersion++;ExpRect=exp;LevelRect=level;Directory.CreateDirectory(Storage.Folder);File.WriteAllText(RegionPath(),string.Join(",",new[]{exp.X,exp.Y,exp.Width,exp.Height,level.X,level.Y,level.Width,level.Height}));}
  string RegionPath(){return Path.Combine(Storage.Folder,"region-"+calibratedSize.Width+"x"+calibratedSize.Height+".txt");}
  void LoadCalibration(){try{if(File.Exists(RegionPath())){var a=File.ReadAllText(RegionPath()).Split(',').Select(int.Parse).ToArray();if(a.Length==8){ExpRect=new Rectangle(a[0],a[1],a[2],a[3]);LevelRect=new Rectangle(a[4],a[5],a[6],a[7]);}}}catch{ExpRect=LevelRect=Rectangle.Empty;}}
  public void StartSession(){if(Target==null&&!demo){Notify("等待游戏");return;}autoStart=false;pauseTimeout.Observe(false,false,0);Session.Start(clock.Elapsed.TotalSeconds);lastGameActivity=clock.Elapsed.TotalSeconds;visited.Clear();if(maps.Fresh(lastGameActivity))visited.Add(maps.Current.Key);Raise();}
  public bool EndSession(){if(!Session.Running)return false;if(!demo)Session.Tick(clock.Elapsed.TotalSeconds);bool saved=!demo&&Storage.Save(Session,MapLabel);Session.Running=false;autoStart=false;Notify(saved?"已保存":"已结束");return saved;}
  public void TogglePause(){if(!Session.Running){StartSession();return;}if(Session.Paused){Session.Resume(clock.Elapsed.TotalSeconds);lastGameActivity=clock.Elapsed.TotalSeconds;}else Session.Pause(clock.Elapsed.TotalSeconds);Raise();}
  public void ImportTable(string path){var data=ParseTable(File.ReadAllLines(path));Directory.CreateDirectory(Storage.Folder);string dest=Path.Combine(Storage.Folder,"levels.csv");if(!string.Equals(Path.GetFullPath(path),dest,StringComparison.OrdinalIgnoreCase))File.Copy(path,dest,true);Session.Requirements.Clear();ExperienceTable.LoadDefaults(Session.Requirements);foreach(var item in data)Session.Requirements[item.Key]=item.Value;}
  static Dictionary<int,long> ParseTable(string[] lines){var result=new Dictionary<int,long>();foreach(string line in lines){if(string.IsNullOrWhiteSpace(line)||line.StartsWith("#"))continue;var p=line.Split(',');int level;long need;if(p.Length!=2||!int.TryParse(p[0],out level)||!long.TryParse(p[1],out need)||level<1||level>300||need<=0)throw new InvalidDataException("格式应为：27,63666，无表头或千位分隔符");result[level]=need;}if(result.Count==0)throw new InvalidDataException("没有有效记录");return result;}
  void LoadTable(){ExperienceTable.LoadDefaults(Session.Requirements);try{string path=Path.Combine(Storage.Folder,"levels.csv");if(File.Exists(path))foreach(var item in ParseTable(File.ReadAllLines(path)))Session.Requirements[item.Key]=item.Value;}catch{Diagnostic="自定义经验表无效，使用内置表";}}
  async Task Pulse(){if(disposed)return;double now=clock.Elapsed.TotalSeconds;if(Target!=null&&Native.GetForegroundWindow()==Target.Handle&&!Native.IsIconic(Target.Handle))lastGameActivity=Math.Max(lastGameActivity,now-Native.InputIdleSeconds());Session.UpdateIdle(now,Math.Max(0,now-lastGameActivity));if(pauseTimeout.Observe(Session.Running,Session.Paused,now))EndSession();Raise();if(busy)return;
   if(Target==null||!Native.IsWindow(Target.Handle)){if(Target!=null){Target=null;ResetLocation();if(Session.Running)Session.Disconnect();}if(now-lastFind>1){lastFind=now;RefreshWindows();}return;}
   IntPtr handle=Target.Handle;if(!Native.IsWindowVisible(handle)||Native.IsIconic(handle)){suspended=true;Diagnostic="游戏已最小化";return;}if(suspended){suspended=false;lastLocate=lastMapScan=-10;failedReads=0;}Rectangle client=Native.ClientBounds(handle);if(client.Width<200||client.Height<200)return;
   if(calibratedSize!=client.Size){sourceVersion++;ExpRect=LevelRect=Rectangle.Empty;calibratedSize=client.Size;lastLocate=-10;LoadCalibration();}
   Reading previous=Session.Last;bool locate=ExpRect.IsEmpty;if(locate&&now-lastLocate<2)return;if(locate)lastLocate=now;Rectangle er=ExpRect,lr=LevelRect;int version=sourceVersion;int stripHeight=Math.Min(300,Math.Max(100,(int)(client.Height*.15)));bool scanMap=now-lastMapScan>=2;if(scanMap)lastMapScan=now;busy=true;
   try{var result=await Task.Run(delegate{var watch=Stopwatch.StartNew();var res=new CaptureResult();try{
     if(ocr==null)ocr=new Ocr(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"runtime"));
     Rectangle stripArea=new Rectangle(client.X,client.Bottom-stripHeight,client.Width,stripHeight);
     if(!locate&&!Native.HudVisible(handle,stripArea,er,lr)){res.Status="状态栏被遮挡";return res;}
     if(scanMap)using(var top=Native.Capture(new Rectangle(client.X,client.Y,client.Width,Math.Min(client.Height,Math.Max(180,client.Height/3))))){Rectangle mr;res.Map=ocr.ReadMap(top,out mr);res.MapSampled=true;if(!mr.IsEmpty&&!Native.RegionVisible(handle,new Rectangle(client.X+mr.X,client.Y+mr.Y,mr.Width,mr.Height)))res.Map=null;}
     res.Strip=Native.Capture(stripArea);
     if(locate&&!ocr.Locate(res.Strip,out er,out lr)){res.Status="未找到状态栏，可在设置中框选";return res;}
     res.Exp=er;res.Level=lr;if(!new Rectangle(Point.Empty,res.Strip.Size).Contains(er)||!new Rectangle(Point.Empty,res.Strip.Size).Contains(lr)){res.Status="区域变化，请重新定位";return res;}
     if(!Native.HudVisible(handle,stripArea,er,lr)){res.Status="状态栏被遮挡";res.Map=null;res.MapSampled=false;res.Exp=res.Level=Rectangle.Empty;return res;}
     using(var e=res.Strip.Clone(er,System.Drawing.Imaging.PixelFormat.Format24bppRgb))using(var l=res.Strip.Clone(lr,System.Drawing.Imaging.PixelFormat.Format24bppRgb))res.Reading=ocr.Read(e,l,previous);
     if(Native.ClientBounds(handle)!=client||!Native.HudVisible(handle,stripArea,er,lr)){res.Reading=null;res.Map=null;res.MapSampled=false;res.Status="状态栏被遮挡";return res;}
     res.Status=res.Reading==null?"数字未读清":"识别正常";return res;
    }catch(Exception ex){res.Status=ex.Message;return res;}finally{res.Ms=watch.Elapsed.TotalMilliseconds;}});
    using(result){if(disposed)return;if(Target==null||Target.Handle!=handle||version!=sourceVersion||calibratedSize!=client.Size||Native.ClientBounds(handle)!=client)return;Diagnostic=result.Status??"等待游戏画面";Latency=result.Ms;if(result.Strip!=null){if(LatestStrip!=null)LatestStrip.Dispose();LatestStrip=(Bitmap)result.Strip.Clone();}if(!result.Exp.IsEmpty){ExpRect=result.Exp;LevelRect=result.Level;}if(result.Reading!=null)failedReads=0;else if(++failedReads>=(result.Status=="状态栏被遮挡"?20:8)){ResetLocation();}if(result.MapSampled&&maps.Observe(result.Map,clock.Elapsed.TotalSeconds)&&Session.Running)visited.Add(maps.Current.Key);if(result.Reading!=null)lastRead=clock.Elapsed.TotalSeconds;if(result.Reading!=null&&autoStart&&!Session.Running)StartSession();bool paused=Session.AutoPaused;if(Session.Running)Session.Accept(result.Reading);if(paused&&!Session.Paused)lastGameActivity=clock.Elapsed.TotalSeconds;Raise();}
   }finally{busy=false;if(disposed&&ocr!=null){ocr.Dispose();ocr=null;}}
  }
  public void Demo(){Session.Requirements.Clear();Session.Requirements[27]=100000;Session.Start(0);long xp=10000;for(int i=0;i<=240;i++){Session.Tick(i*5);xp+=i/12==6?-40:20+(i/12%5)*60;var r=new Reading{Level=27,Exp=xp,Percent=Math.Round(xp/100000.0*100,2),Confidence=95};Session.Accept(r);Session.Accept(r);}maps.Observe(new MapReading{Region="Victoria Road",Name="Henesys"},0);maps.Observe(new MapReading{Region="Victoria Road",Name="Henesys"},2);visited.Add("Victoria Road / Henesys");}
  public void Dispose(){if(disposed)return;disposed=true;timer.Stop();if(LatestStrip!=null){LatestStrip.Dispose();LatestStrip=null;}if(!busy&&ocr!=null){ocr.Dispose();ocr=null;}}
 }
}
