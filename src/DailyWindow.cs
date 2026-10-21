using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using MapleMeter;
namespace MapleWarm {
 public sealed class DailyWindow:Surface {
  readonly Engine engine;readonly bool demo;readonly DispatcherTimer timer=new DispatcherTimer();int days=7;bool closed;ScrollViewer records;
  public DailyWindow(Engine engine=null,bool demo=false):base("MapleMeter · 每日统计",640,600){this.engine=engine;this.demo=demo;Build();App.FontChanged+=Build;timer.Interval=TimeSpan.FromSeconds(5);timer.Tick+=delegate{if(IsVisible)Build();};timer.Start();Closed+=delegate{closed=true;timer.Stop();App.FontChanged-=Build;};}
  TextBlock Label(string text,double size,double x,double y,double w,double h,Brush color=null,bool bold=false,TextAlignment align=TextAlignment.Left){var t=App.Text(text,size,color,bold);t.TextAlignment=align;return App.At(canvas,t,x,y,w,h);}
  void Build(){if(closed)return;double offset=records==null?0:records.VerticalOffset;Reset();Header("每日经验统计");int skipped=0;var values=demo?Enumerable.Range(0,12).Select(i=>new DailyValue{Date=DateTime.Today.AddDays(-i),Gain=32000+(i*17391)%81000,Seconds=1200+i*91}).ToList():DailyStorage.Load(Storage.Folder,engine==null?null:engine.Session,out skipped);var byDate=values.ToDictionary(x=>x.Date);var period=Enumerable.Range(0,days).Select(i=>{var date=DateTime.Today.AddDays(-i);DailyValue d;return byDate.TryGetValue(date,out d)?d:new DailyValue{Date=date};}).ToList();var today=period[0];var yesterday=period[1];
   App.At(canvas,App.Card(),20,52,600,86);string[] labels={"今日净经验","较昨日","今日有效时长"};string[] numbers={App.Number(today.Gain),(today.Gain-yesterday.Gain>0?"+":"")+App.Number(today.Gain-yesterday.Gain),App.Duration(today.Seconds)};for(int i=0;i<3;i++){Label(labels[i],12,28+i*200,60,184,24,App.Muted,false,TextAlignment.Center);Label(numbers[i],23,28+i*200,89,184,34,i==2?App.Ink:i==1&&today.Gain<yesterday.Gain?App.Brush("#ED856D"):App.Green,true,TextAlignment.Center);}
   Label("每天净经验",14,24,150,250,30,null,true);App.At(canvas,App.Button(days==7?"最近 7 天 ▾":"最近 30 天 ▾",null,()=>{days=days==7?30:7;Build();}),478,150,142,30);
   var plot=new Canvas();App.At(canvas,App.Card(plot,null,12,true),20,190,600,155);double max=Math.Max(1,period.Max(x=>(double)Math.Max(0,x.Gain))),min=Math.Min(0,period.Min(x=>(double)x.Gain)),span=max-min,zero=28+max/span*92,step=552.0/days;App.At(plot,new Rectangle{Fill=App.Line},24,zero,552,1);var chronological=period.AsEnumerable().Reverse().ToList();for(int i=0;i<days;i++){var d=chronological[i];double top=28+(max-d.Gain)/span*92;var bar=new Border{Background=d.Gain<0?App.Brush("#ED856D"):App.Gradient("#A1B78B","#8AA574"),CornerRadius=new CornerRadius(3),ToolTip=d.Date.ToString("yyyy-MM-dd")+" · "+App.Number(d.Gain)+" EXP"};App.At(plot,bar,24+i*step+4,Math.Min(top,zero),step-8,Math.Max(2,Math.Abs(zero-top)));if(days==7){var amount=App.Text(App.Number(d.Gain),10,App.Muted);amount.TextAlignment=TextAlignment.Center;App.At(plot,amount,24+i*step,Math.Max(0,Math.Min(top,zero)-21),step,18);}if(days==7||i%5==0||i==days-1){var text=App.Text(d.Date.ToString("MM/dd"),10,App.Muted);text.TextAlignment=TextAlignment.Center;App.At(plot,text,24+i*step-6,126,step+12,22);}}
   var rows=new StackPanel();rows.Children.Add(Row(new[]{"日期","净经验","较前一天","有效时长"},true));foreach(var d in period){DailyValue previous;long before=byDate.TryGetValue(d.Date.AddDays(-1),out previous)?previous.Gain:0;rows.Children.Add(Row(new[]{d.Date.ToString("MM-dd")+(d.Approximate?" ≈":""),App.Number(d.Gain),(d.Gain-before>0?"+":"")+App.Number(d.Gain-before),App.Duration(d.Seconds)}));}records=App.At(canvas,new ScrollViewer{Content=rows,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled},20,359,600,190);UpdateLayout();records.ScrollToVerticalOffset(offset);
   Label(skipped>0?skipped+" 条损坏记录已跳过":"含短时记录 · 今日尚未结束 · 无记录日按 0 对比",11,24,557,592,25,App.Muted).ToolTip="按本地日期统计净经验（已扣除损失）。跨日按确认读数归属日期。旧记录以开始日归档并标记 ≈，无法补回旧版未保存的短时记录。";
  }
  Grid Row(string[] values,bool header=false){var g=new Grid{Height=header?30:34,Background=header?App.Brush("#F8EEE1"):Brushes.Transparent};double[] widths={110,165,165,140};for(int i=0;i<4;i++){g.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(widths[i])});var text=App.Text(values[i],12,header?App.Muted:(i==1||i==2)&&values[i].StartsWith("-")?App.Brush("#ED856D"):i==1?App.Green:App.Ink,false,!header);text.TextAlignment=i==0?TextAlignment.Left:TextAlignment.Right;var cell=new Border{Child=text,Padding=new Thickness(10,0,12,0),BorderBrush=App.Line,BorderThickness=new Thickness(0,0,0,1)};Grid.SetColumn(cell,i);g.Children.Add(cell);}return g;}
 }
}
