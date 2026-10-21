using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MapleMeter;
namespace MapleWarm {
 public sealed class MinuteGraph:Canvas {
  List<MinuteData> items=new List<MinuteData>();bool live;int offset;string signature="";
  public MinuteGraph(){SizeChanged+=delegate{Draw();};Background=Brushes.Transparent;ToolTip="每格是一分钟有效时间；滚轮查看更早记录";MouseWheel+=delegate(object s,MouseWheelEventArgs e){offset=Math.Max(0,Math.Min(Math.Max(0,items.Count-30),offset+(e.Delta>0?5:-5)));Draw();e.Handled=true;};}
  public void SetData(List<MinuteData> data,bool running){data=data??new List<MinuteData>();string key=running+"|"+string.Join(";",data.Select(x=>x.Index+","+x.Gain+","+(int)x.Seconds+","+x.Gap+","+x.Estimated));if(key==signature)return;signature=key;if(data.Count<items.Count)offset=0;else if(offset>0)offset+=data.Count-items.Count;items=data;live=running;Draw();}
  static string Short(double value){return Math.Abs(value)>=1000000?(value/1000000).ToString("0.#")+"M":Math.Abs(value)>=1000?(value/1000).ToString("0.#")+"K":value.ToString("0");}
  void Draw(){Children.Clear();double width=ActualWidth>0?ActualWidth:Width,height=ActualHeight>0?ActualHeight:Height;if(double.IsNaN(width)||width<80||height<60)return;if(items.Count==0){var empty=App.Text("等待经验数据",12,App.Muted);empty.TextAlignment=TextAlignment.Center;App.At(this,empty,0,0,width,height);return;}var visible=items.Skip(Math.Max(0,items.Count-30-offset)).Take(30).ToList();double left=42,right=width-5,top=12,bottom=height-26,max=Math.Max(1,visible.Where(x=>!x.Gap).Select(x=>(double)Math.Max(0,x.Gain)).DefaultIfEmpty(1).Max()),min=Math.Min(0,visible.Where(x=>!x.Gap).Select(x=>(double)x.Gain).DefaultIfEmpty(0).Min()),span=max-min,zero=top+max/span*(bottom-top);double step=(right-left)/Math.Max(10,visible.Count);
   foreach(double v in new[]{max,0,min}.Distinct()){double y=top+(max-v)/span*(bottom-top);App.At(this,new Rectangle{Fill=App.Line},left,y,right-left,1);var label=App.Text(Short(v),10,App.Muted,false,true);label.TextAlignment=TextAlignment.Right;App.At(this,label,0,y-9,32,18);}
   for(int i=0;i<visible.Count;i++){var b=visible[i];double y=top+(max-b.Gain)/span*(bottom-top),h=Math.Max(2,Math.Abs(zero-y));bool current=live&&b.Index==items.Last().Index&&b.Seconds<60;Brush fill=b.Gain<0?App.Gradient("#F4A085","#ED856D"):current?App.Gradient("#FFC467","#FFAB35"):App.Gradient("#A1B78B","#8AA574");if(b.Gap){var drawing=new GeometryDrawing(null,new Pen(App.Muted,1),Geometry.Parse("M0,8 L8,0"));fill=new DrawingBrush(drawing){TileMode=TileMode.Tile,Viewport=new Rect(0,0,8,8),ViewportUnits=BrushMappingMode.Absolute};}
    var bar=new Border{Background=fill,CornerRadius=new CornerRadius(2,2,0,0),ToolTip="第 "+(b.Index+1)+" 分钟 · "+b.Seconds.ToString("0")+" 秒\n净经验 "+b.Gain.ToString("N0")+(b.Gap?"\n含识别中断，分钟归属不确定":"")+(b.Estimated?"\n含估算":"")};App.At(this,bar,left+i*step+2,b.Gap?top:b.Gain>=0?zero-h:zero,Math.Max(3,step-5),b.Gap?bottom-top:h);
    if(i==0||i==visible.Count-1||(b.Index+1)%5==0){var label=App.Text((b.Index+1).ToString(),10,App.Muted,false,true);label.TextAlignment=TextAlignment.Center;App.At(this,label,left+i*step,height-22,step,20);}
   }
  }
 }
}
