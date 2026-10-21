using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MapleMeter {
 static class Tests {
  static List<string> results=new List<string>();
  static void Check(bool ok,string name){results.Add((ok?"PASS ":"FAIL ")+name);if(!ok)throw new Exception(name);}
  static Reading R(long xp,int lv=27){return new Reading{Exp=xp,Level=lv,Percent=Math.Round(xp/100000.0*100,2),Confidence=95};}
  static void Feed(Session s,Reading r,double time){s.Tick(time);s.Accept(r);s.Tick(time+.25);s.Accept(r);}
  public static void Run(string path){
   try {
    var builtin=new Dictionary<int,long>();ExperienceTable.LoadDefaults(builtin);Check(builtin.Count==99&&builtin[27]==63666&&builtin[28]==73080&&builtin[99]==9482161548L&&!builtin.ContainsKey(100),"verified built-in range and level indexing");
    for(int lv=1;lv<100;lv++){var cross=new Session();ExperienceTable.LoadDefaults(cross.Requirements);cross.Start(0);long before=builtin[lv]-1;var oldReading=new Reading{Level=lv,Exp=before,Percent=System.Math.Floor(before*10000.0/builtin[lv])/100,Confidence=95};cross.Accept(oldReading);cross.Accept(oldReading);cross.Tick(1);var builtinNext=new Reading{Level=lv+1,Exp=1,Percent=lv==99?0:System.Math.Floor(10000.0/builtin[lv+1])/100,Confidence=95};cross.Accept(builtinNext);cross.Accept(builtinNext);Check(cross.Gain==2&&cross.Loss==0&&!cross.Estimated&&!cross.Incomplete,"exact built-in level transition "+lv);}
    Check(builtin.Values.Sum()==18194778397L,"level 1 to 100 cumulative EXP fits Int64");
    var multi=new Session();ExperienceTable.LoadDefaults(multi.Requirements);multi.Start(0);var prior=new Reading{Level=27,Exp=63665,Percent=99.99,Confidence=95};multi.Accept(prior);multi.Accept(prior);multi.Tick(5);var after=new Reading{Level=29,Exp=100,Percent=.11,Confidence=95};multi.Accept(after);multi.Accept(after);Check(multi.Gain==73181&&!multi.Estimated,"exact multi-level continuity");

    Check(Ocr.ParseScreen("9391.28%","28",95)==null,"ambiguous concatenated EXP rejected");
    Check(Ocr.ParseScreen("939 1.28%","28",95).Exp==939,"one-digit percent separated from EXP");
    foreach(var pct in new[]{"0.00","0.01","1.28","9.99","10.00","99.99"})Check(Math.Abs(Ocr.ParseScreen("939 ["+pct+"%]","28",95).Percent-double.Parse(pct,System.Globalization.CultureInfo.InvariantCulture))<.0001,"percent boundary "+pct);


    var timeout=new PauseTimeout();Check(!timeout.Observe(true,true,0)&&!timeout.Observe(true,true,899.99)&&timeout.Observe(true,true,900),"pause ends at fifteen minutes");timeout.Observe(true,false,901);Check(!timeout.Observe(true,true,1000)&&!timeout.Observe(true,true,1899)&&timeout.Observe(true,true,1900),"resume resets pause timeout");timeout.Observe(false,true,2000);Check(!timeout.Observe(true,true,3000),"ended session resets pause timeout");
    var midnight=new Session();midnight.Start(0);midnight.Started=new DateTime(2026,10,17,23,59,50);Feed(midnight,R(10000),0);Feed(midnight,R(10500),5);Feed(midnight,R(11000),15);Check(midnight.Days.Count==2&&midnight.Days[0].Gain==500&&midnight.Days[1].Gain==500,"daily EXP splits at local midnight");Check(Math.Abs(midnight.Days[0].Seconds-10)<.001&&Math.Abs(midnight.Days.Sum(x=>x.Seconds)-midnight.Seconds)<.001,"daily active seconds split at midnight");
    string dailyFolder=Path.Combine(Path.GetTempPath(),"maplemeter-daily-"+Guid.NewGuid().ToString("N"));try{Check(!Storage.SaveTo(dailyFolder,midnight,"test"),"short session excluded from history");int skippedDaily;var daily=DailyStorage.Load(dailyFolder,null,out skippedDaily);Check(daily.Sum(x=>x.Gain)==1000&&daily.Count==2,"short session included in daily totals");DailyStorage.Save(dailyFolder,midnight);Check(DailyStorage.Load(dailyFolder,midnight,out skippedDaily).Sum(x=>x.Gain)==1000,"daily overwrite and live session do not double count");}finally{if(Directory.Exists(dailyFolder))Directory.Delete(dailyFolder,true);}
    var numbered=new MapTracker();numbered.Observe(MapReading.Parse("Hidden Street\nHidden Tree 1",95),0);numbered.Observe(MapReading.Parse("Hidden Street\nHidden Tree 1",95),2);numbered.Observe(MapReading.Parse("Hidden Street\nHidden Tree 2",95),4);Check(numbered.Observe(MapReading.Parse("Hidden Street\nHidden Tree 2",95),6)&&numbered.Current.Name=="Hidden Tree 2","numbered maps remain distinct after confirmation");

    var r=Ocr.Parse("EXP: 23,393 [36.74%]","LV. 27",90);Check(r!=null&&r.Exp==23393&&r.Level==27&&r.Percent==36.74,"parse supplied HUD text");
    Check(Ocr.Parse("HP 838/1030","27",90)==null,"reject HP instead of EXP");
    Check(Ocr.Parse("23393","27",90)==null,"reject integer without percent");
    Check(Ocr.Parse("23.39326.74%","27",90)==null,"reject malformed number instead of parsing a substring");
    Check(Ocr.Parse("23,39336.74%","27",90).Exp==23393,"parse pixel glyph stream without brackets");
    var s=new Session();s.Requirements[27]=100000;s.Requirements[28]=100000;s.Start(0);Feed(s,R(10000),0);Feed(s,R(11000),10);Check(s.Gain==1000,"same-level exact gain");
    s.Tick(11);s.Accept(R(90000));s.Tick(12);s.Accept(R(11000));s.Tick(12.25);s.Accept(R(11000));Check(s.Gain==1000,"single-frame spike rejected");
    s.Pause(15);s.Tick(25);Check(Math.Abs(s.Seconds-15)<.01,"pause excludes elapsed time");s.Resume(25);Feed(s,R(15000),26);Check(s.Gain==1000,"resume discards paused gain");Feed(s,R(16000),30);Check(s.Gain==2000,"gain after resume");
    Feed(s,R(15000),35);Check(s.Gain==1000&&s.Loss==1000,"confirmed EXP decrease is net loss");
    Feed(s,R(99000),40);Feed(s,R(1000,28),45);Check(s.Gain==87000&&!s.Estimated,"exact level-up continuity");
    s.Tick(80);Check(double.IsNaN(s.Rate(300)),"stale rate hidden");
    var z=new Session();z.Start(0);Feed(z,new Reading{Exp=0,Level=1,Percent=0,Confidence=90},0);Feed(z,R(100,2),5);Check(z.Incomplete,"missing level requirement marks incomplete");
    var a=new Session();a.Start(0);Feed(a,R(90000),0);Feed(a,R(1000,28),5);Check(a.Estimated&&Math.Abs(a.Gain-11000)<20,"screen-derived level-up is explicitly estimated");
    var bad=R(50000);bad.Percent=1;s.Tick(81);s.Accept(bad);s.Accept(bad);Check(s.Last.Exp==1000,"percent cross-check blocks contradictory sample");
    var idle=new Session();idle.Requirements[27]=100000;idle.Start(0);Feed(idle,R(10000),0);idle.UpdateIdle(14.99,14.99);Check(!idle.Paused,"idle below fifteen seconds keeps recording");
    idle.UpdateIdle(16,16);Check(idle.Paused&&idle.AutoPaused&&Math.Abs(idle.Seconds-15)<.001,"idle cutoff excludes time after fifteen seconds");
    idle.UpdateIdle(30,30);idle.Accept(R(10000));idle.Accept(R(10000));Check(idle.Paused&&idle.Seconds==15,"unchanged experience does not resume or add time");
    idle.Accept(null);idle.Accept(R(11000));idle.Accept(R(10000));Check(idle.Paused&&idle.Gain==0,"single changed frame cannot resume auto-pause");
    idle.Accept(R(11000));idle.Accept(R(11000));Check(!idle.Paused&&!idle.AutoPaused&&idle.Gain==1000,"confirmed EXP change resumes and counts triggering gain");
    idle.UpdateIdle(31,1);Check(Math.Abs(idle.Seconds-16)<.001,"resumed timer excludes paused interval");
    idle.Pause(32);idle.Accept(R(12000));idle.Accept(R(12000));Check(idle.Paused&&idle.Gain==1000,"explicit manual pause is not overridden");
    var death=new Session();death.Requirements[27]=100000;death.Start(0);Feed(death,R(10000),0);death.UpdateIdle(20,20);death.Accept(R(9000));death.Accept(R(9000));Check(!death.Paused&&death.Gain==-1000&&death.Loss==1000,"negative EXP change also resumes once");
    var levelIdle=new Session();levelIdle.Requirements[27]=100000;levelIdle.Requirements[28]=100000;levelIdle.Start(0);Feed(levelIdle,R(99000),0);levelIdle.UpdateIdle(20,20);levelIdle.Accept(R(1000,28));levelIdle.Accept(R(1000,28));Check(!levelIdle.Paused&&levelIdle.Gain==2000,"level-up resumes without losing boundary experience");
    var tracker=new MapTracker();var town=MapReading.Parse("Victoria Road\nHenesys\n",90);Check(town!=null&&town.Name=="Henesys","parse two-line minimap title");
    Check(!tracker.Observe(town,0)&&tracker.Observe(town,2),"map requires two stable reads");
    var next=MapReading.Parse("Victoria Road\nEllinia",90);tracker.Observe(next,4);tracker.Observe(town,6);Check(tracker.Current.Name=="Henesys","map flicker does not replace current map");
    tracker.Observe(next,8);Check(tracker.Observe(next,10)&&tracker.Current.Name=="Ellinia","confirmed new map replaces current map");
    Check(tracker.Fresh(100),"switching to tool retains last confirmed map");
    tracker.Observe(null,12);tracker.Observe(null,14);tracker.Observe(null,16);Check(!tracker.Fresh(18),"repeated unreadable minimap hides stale location");
    Check(MapReading.Parse("Victoria Road\nHenesys",20)==null&&MapReading.Parse("123\n456",90)==null,"reject low confidence and non-map text");
    var series=new MinuteSeries();series.Add(59.9,100,false);series.Add(60,200,false);series.Add(61,-50,false);var bars=series.Snapshot(90,true,90);
    Check(bars.Count==2&&bars[0].Gain==100&&bars[1].Gain==150&&bars[1].Seconds==30,"minute boundary and partial bucket preserve exact net gains");
    series.Add(125,-500,false);bars=series.Snapshot(130,true,130);Check(bars[2].Gain==-500,"negative minute remains negative");
    series.MarkGap(55,65);bars=series.Snapshot(130,true,130);Check(bars[0].Gap&&bars[1].Gap&&!bars[2].Gap,"capture gap flags crossed buckets without inventing values");
    Check(bars.Sum(b=>b.Gain)==-250,"minute totals conserve session net experience");
    var pausedBars=idle.Minutes;idle.Tick(500);Check(idle.Minutes.Count==pausedBars.Count,"paused session does not advance minute bins");
    string fixture=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path)),"history-fixture-"+Guid.NewGuid().ToString("N"));
    var hs=new Session();hs.Start(0);Feed(hs,R(10000),0);hs.Tick(600);Check(!Storage.Eligible(hs)&&!Storage.SaveTo(fixture,hs,"short")&&!Directory.Exists(fixture),"exactly ten minutes never writes history");
    hs.Tick(601);hs.Accept(R(11000));hs.Accept(R(11000));Check(Storage.Eligible(hs)&&Storage.SaveTo(fixture,hs,"Road, \"Map\"\nFloor 1"),"over ten minutes writes structured history");
    Storage.SaveTo(fixture,hs,"duplicate");int skipped;var history=Storage.Load(fixture,out skipped);Check(history.Count==1&&skipped==0&&history[0].Map=="Road, \"Map\"\nFloor 1"&&history[0].Minutes.Sum(b=>b.Gain)==1000,"structured history roundtrip and duplicate protection");
    File.WriteAllText(Path.Combine(fixture,"sessions.csv"),"开始时间,地图,计入秒数,净经验,经验减少,平均每小时,比例,估算,缺失\n2026-10-18 10:00:00,短,600,10,0,60,90,否,否\n2026-10-18 11:00:00,\"A, B\",900,1000,0,4000,90,是,否\n");
    File.WriteAllText(Path.Combine(fixture,"history","broken.xml"),"broken");history=Storage.Load(fixture,out skipped);Check(history.Count==2&&skipped==1&&history.Any(x=>x.Legacy&&x.Map=="A, B"),"legacy short rows filtered and corrupt record isolated");
    Check(HistoryForm.Filter(history,0,"a, b",0,DateTime.Now).Count==1,"history search matches maps case insensitively");
    Check(Math.Abs(HistoryForm.WeightedRate(new[]{new HistoryRecord{Seconds=900,Gain=1000},new HistoryRecord{Seconds=1800,Gain=8000}})-12000)<.01,"history aggregate uses time-weighted rate");
    Storage.Export(Path.Combine(fixture,"export.csv"),history);Check(Storage.ParseCsv(File.ReadAllText(Path.Combine(fixture,"export.csv"))).Count==3,"export quotes multiline and comma-containing map names");
    results.Add("ALL TESTS PASSED");
   }catch(Exception ex){results.Add("ERROR "+ex.Message);Environment.ExitCode=1;}
   File.WriteAllLines(path,results);
  }
 }
}
