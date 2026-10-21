using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MapleWarm {
 public class App : Application {
  public static string Dir=AppDomain.CurrentDomain.BaseDirectory;
  public static double UiScale=1;
  public static double LayoutScale{get{return .9*UiScale;}}
  public static FontWeight BodyWeight{get{return Wen?FontWeights.Bold:FontWeights.Medium;}}
  public static void SetScale(double scale,bool save=true){if(scale!=.8&&scale!=.9&&scale!=1&&scale!=1.25&&scale!=1.5)return;if(save){Directory.CreateDirectory(MapleMeter.Storage.Folder);File.WriteAllText(System.IO.Path.Combine(MapleMeter.Storage.Folder,"scale.txt"),scale.ToString(System.Globalization.CultureInfo.InvariantCulture));}UiScale=scale;RefreshAppearance();}
  public static bool Wen=false;public static event Action FontChanged;public static void SetFont(bool value,bool save=true){if(save){Directory.CreateDirectory(MapleMeter.Storage.Folder);File.WriteAllText(System.IO.Path.Combine(MapleMeter.Storage.Folder,"font.txt"),value?"c":"a");}Wen=value;if(FontChanged!=null)FontChanged();}
  public static void RefreshAppearance(){Ink=Brush("#4A210F");Muted=Brush("#90715B");Line=Brush("#EED8BC");Green=Brush("#226E36");if(FontChanged!=null)FontChanged();}
  public static string Number(double n){return double.IsNaN(n)||double.IsInfinity(n)?"—":n.ToString("N0",System.Globalization.CultureInfo.InvariantCulture);}
  public static string Duration(double seconds){var t=TimeSpan.FromSeconds(seconds);return ((int)t.TotalHours).ToString("00")+t.ToString(@"\:mm\:ss");}
  public static Brush Ink=Brush("#4A210F"),Muted=Brush("#90715B"),Line=Brush("#EED8BC"),Green=Brush("#226E36");
  public static Brush Brush(string s){return (Brush)new BrushConverter().ConvertFromString(Theme.Color(s));}
  public static FontFamily Chinese{get{return new FontFamily(new Uri(Dir+"fonts/"),"./#"+(Wen?"MM Wen UI":"MM Sans UI"));}}
  public static FontFamily Latin=new FontFamily(new Uri(Dir+"fonts/"),"./#Inter");
  public static LinearGradientBrush Gradient(string a,string b){return new LinearGradientBrush((Color)ColorConverter.ConvertFromString(Theme.Color(a)),(Color)ColorConverter.ConvertFromString(Theme.Color(b)),90);}
  public static void SetMapText(TextBlock text,string value,bool bold){bool chinese=false;foreach(char c in value){if(c>127&&char.IsLetter(c)){chinese=true;break;}}text.Text=value;text.FontFamily=chinese?Chinese:Latin;text.FontWeight=bold||(chinese&&Wen)?FontWeights.Bold:FontWeights.Medium;}
  public static TextBlock Text(string s,double size=16,Brush color=null,bool bold=false,bool latin=false){return new TextBlock{Text=s,FontSize=size,Foreground=color??Ink,FontFamily=latin?Latin:Chinese,FontWeight=bold||(!latin&&Wen)?FontWeights.Bold:FontWeights.Medium,TextTrimming=TextTrimming.CharacterEllipsis,VerticalAlignment=VerticalAlignment.Center};}
  public static T At<T>(Canvas c,T child,double x,double y,double width=double.NaN,double height=double.NaN)where T:FrameworkElement{FrameworkElement placed=child;var text=child as TextBlock;if(text!=null){var cell=new Grid{Height=double.IsNaN(height)?Math.Ceiling(text.FontSize*1.6):height,Width=width};text.VerticalAlignment=VerticalAlignment.Center;cell.Children.Add(text);placed=cell;}else{if(!double.IsNaN(width))child.Width=width;if(!double.IsNaN(height))child.Height=height;}Canvas.SetLeft(placed,x);Canvas.SetTop(placed,y);c.Children.Add(placed);return child;}
  public static Border Card(UIElement child=null,Brush fill=null,double radius=16,bool border=false){return new Border{Child=child,Background=fill??Gradient("#FFFDF6","#FCF5E8"),CornerRadius=new CornerRadius(radius),BorderBrush=Line,BorderThickness=new Thickness(border?1:0)};}
  public static UIElement Icon(string name,double size,Brush color){if(name=="leaf"&&Theme.Warm&&File.Exists(System.IO.Path.Combine(Dir,"icon.png")))return new Image{Source=Assets.Icon,Width=size,Height=size};string data;
   switch(name){case "leaf":data="M 12,0 L 15,7 20,4 19,11 24,10 22,16 14,20 13,24 11,24 11,20 2,16 0,10 5,11 4,4 9,7 Z";break;
    case "pin":data="M12,24 C10,20 2,14 2,9 A10,10 0 1 1 22,9 C22,14 14,20 12,24 M12,5 A4,4 0 1 0 12,13 A4,4 0 1 0 12,5";break;
    case "stop":data="M4,3 L20,3 Q22,3 22,5 L22,21 Q22,23 20,23 L4,23 Q2,23 2,21 L2,5 Q2,3 4,3 Z";break;
    case "float":data="M1,4 L18,4 18,18 1,18 Z M7,10 L24,10 24,24 7,24 Z";break;
    case "history":data="M5,2 L23,2 23,23 5,23 Z M1,6 L5,6 M1,11 L5,11 M1,16 L5,16 M10,7 L18,7 M10,12 L18,12 M10,17 L16,17";break;
    case "settings":data="M9,1 L15,1 16,5 20,7 23,7 25,12 22,15 21,19 18,22 14,21 10,23 6,20 5,16 1,13 3,8 7,7 Z M13,8 A5,5 0 1 0 13,18 A5,5 0 1 0 13,8";break;
    case "clock":data="M12,1 A11,11 0 1 0 12,23 A11,11 0 1 0 12,1 M12,5 L12,12 17,15";break;
    case "back":data="M9,1 L1,1 1,23 23,23 23,15 M12,1 L23,1 23,12 M23,1 L11,13";break;
    default:data="M2,12 L22,12";break;
   }
   bool filled=name=="leaf"||name=="pin"||name=="stop";var path=new System.Windows.Shapes.Path{Data=Geometry.Parse(data),Fill=filled?color:null,Stroke=filled?null:color,StrokeThickness=2,StrokeLineJoin=PenLineJoin.Round,Stretch=Stretch.Uniform};return new Viewbox{Width=size,Height=size,Child=path};
  }
  public static Button Button(string label,string icon,Action action,bool primary=false){var row=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};Brush ink=primary?Brushes.White:Ink;if(icon!=null){var glyph=Icon(icon,17,ink);((FrameworkElement)glyph).Margin=new Thickness(0,0,8,0);row.Children.Add(glyph);}row.Children.Add(Text(label,14,ink,false));var button=new Button{Tag=label,Content=row,Cursor=Cursors.Hand,Background=primary?Gradient("#F37750","#DE5834"):Gradient("#FFFCF5","#FCF4E7"),Foreground=ink,BorderBrush=primary?Brush("#E56543"):Line,BorderThickness=new Thickness(1),Padding=new Thickness(12,8,12,8)};
   var border=new FrameworkElementFactory(typeof(Border));border.SetValue(Border.CornerRadiusProperty,new CornerRadius(12));border.SetBinding(Border.BackgroundProperty,new System.Windows.Data.Binding("Background"){RelativeSource=new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)});border.SetBinding(Border.BorderBrushProperty,new System.Windows.Data.Binding("BorderBrush"){RelativeSource=new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)});border.SetValue(Border.BorderThicknessProperty,new Thickness(1));var presenter=new FrameworkElementFactory(typeof(ContentPresenter));presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty,HorizontalAlignment.Center);presenter.SetValue(ContentPresenter.VerticalAlignmentProperty,VerticalAlignment.Center);border.AppendChild(presenter);button.Template=new ControlTemplate(typeof(Button)){VisualTree=border};button.MouseEnter+=delegate{button.Opacity=.85;};button.MouseLeave+=delegate{button.Opacity=1;};button.Click+=delegate{action();};return button;
  }
  [STAThread] public static void Main(string[] args){MapleMeter.Native.SetProcessDPIAware();try{if(args.Length>2&&args[0]=="--integration-test"){MapleMeter.IntegrationTests.Run(args[1],args[2]);return;}if(args.Length>0&&args[0]=="--self-test"){MapleMeter.Tests.Run(args.Length>1?args[1]:System.IO.Path.Combine(Dir,"tests.txt"));return;}try{double scale;if(double.TryParse(File.ReadAllText(System.IO.Path.Combine(MapleMeter.Storage.Folder,"scale.txt")),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out scale)&&(scale==.8||scale==.9||scale==1||scale==1.25||scale==1.5))UiScale=scale;}catch{}Theme.Load();if(args.Contains("--mint"))Theme.Id="mint";if(args.Contains("--sky"))Theme.Id="sky";RefreshAppearance();bool demo=args.Contains("--render")||args.Contains("--demo");try{Wen=File.ReadAllText(System.IO.Path.Combine(MapleMeter.Storage.Folder,"font.txt")).Trim()=="c";}catch{}if(args.Contains("--font-c"))Wen=true;bool ownsMutex;var instance=new System.Threading.Mutex(true,"Local\\MapleMeter-Warm",out ownsMutex);if(!ownsMutex&&!demo)return;var app=new App();app.Exit+=delegate{if(ownsMutex)instance.ReleaseMutex();instance.Dispose();};app.ShutdownMode=ShutdownMode.OnMainWindowClose;var main=new Meter(demo);app.MainWindow=main;if(args.Contains("--render")){app.ShutdownMode=ShutdownMode.OnExplicitShutdown;main.Show();main.UpdateLayout();main.RenderAll();main.Close();app.Shutdown();return;}if(args.Contains("--smoke")){var timer=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromSeconds(8)};timer.Tick+=delegate{timer.Stop();main.Save("startup.png");File.WriteAllText(System.IO.Path.Combine(Dir,"startup-check.txt"),"PASS production startup; status="+main.Engine.Status+"; running="+main.Engine.Session.Running+"; storage="+MapleMeter.Storage.Folder);main.Close();};timer.Start();}app.Run(main);}catch(Exception ex){File.WriteAllText(System.IO.Path.Combine(Dir,"error.log"),ex.ToString());if(!args.Any(x=>x.EndsWith("test")||x=="--render"||x=="--smoke"))MessageBox.Show("运行失败："+ex.Message,"MapleMeter 暖光版");Environment.ExitCode=1;}}
 }
 public class Surface:Window {
  protected Canvas canvas;protected double bodyWidth,bodyHeight;
  public Surface(string title,double w,double h){Title=title;bodyWidth=w;bodyHeight=h;Width=w+32;Height=h+32;WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterScreen;FontFamily=App.Chinese;UseLayoutRounding=true;if(File.Exists(System.IO.Path.Combine(App.Dir,"icon.png")))Icon=Assets.Icon;TextOptions.SetTextFormattingMode(this,TextFormattingMode.Display);TextOptions.SetTextRenderingMode(this,TextRenderingMode.ClearType);Reset();}
  protected void Reset(){Width=bodyWidth*App.LayoutScale+32;Height=bodyHeight*App.LayoutScale+32;canvas=new Canvas{Width=bodyWidth,Height=bodyHeight};var shell=App.Card(canvas,App.Gradient("#FFFDF7","#FFF9EF"),20,true);canvas.LayoutTransform=new ScaleTransform(App.LayoutScale,App.LayoutScale);shell.Margin=new Thickness(16);shell.Effect=new DropShadowEffect{BlurRadius=24,ShadowDepth=6,Direction=270,Opacity=.18,Color=Theme.Warm?Color.FromRgb(128,83,34):Color.FromRgb(12,29,49)};Content=shell;}
  protected void Header(string title,bool small=false){var drag=new Border{Background=Brushes.Transparent};App.At(canvas,drag,0,0,bodyWidth-90,44);canvas.MouseLeftButtonDown+=delegate(object s,MouseButtonEventArgs e){if(e.GetPosition(canvas).Y<44&&e.ButtonState==MouseButtonState.Pressed){DragMove();e.Handled=true;}};App.At(canvas,(FrameworkElement)App.Icon("leaf",22,App.Brush("#EB7848")),20,13);App.At(canvas,App.Text(title,17,null,true),52,7,bodyWidth-170,30);
   var min=App.Button("−",null,()=>WindowState=WindowState.Minimized);var close=App.Button("×",null,()=>Close());min.Background=close.Background=Brushes.Transparent;min.BorderBrush=close.BorderBrush=Brushes.Transparent;App.At(canvas,min,bodyWidth-86,6,32,32);App.At(canvas,close,bodyWidth-46,6,32,32);}
  public void ClickButton(string label){var button=canvas.Children.OfType<Button>().First(b=>Equals(b.Tag,label));button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}
  public RenderTargetBitmap Snapshot(){UpdateLayout();var result=new RenderTargetBitmap((int)Width,(int)Height,96,96,PixelFormats.Pbgra32);result.Render(this);return result;}
  public void Save(string name){var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(Snapshot()));using(var file=File.Create(System.IO.Path.Combine(App.Dir,name)))png.Save(file);}
 }

}
