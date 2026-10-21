using System;
using System.Drawing;
using System.Windows.Forms;
namespace MapleMeter {
 public sealed class CalibrationForm : Form {
  readonly Bitmap source;readonly PictureBox picture=new PictureBox();readonly Label tip=new Label();Point origin;bool dragging;int step;Rectangle selection;public Rectangle Exp,Level;
  public CalibrationForm(Bitmap image,Rectangle exp,Rectangle level) {
   source=(Bitmap)image.Clone();Exp=exp;Level=level;Text="识别预览 · 先框经验，再框等级";Size=new Size(1100,440);MinimumSize=new Size(700,400);StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(255,249,239);ForeColor=Color.FromArgb(74,33,15);
   tip.Text="绿色：经验区域   金色：等级区域。重新框选：先拖出包含完整经验数字和百分比的矩形，再框等级数字。";tip.Dock=DockStyle.Top;tip.Height=60;
   picture.Dock=DockStyle.Fill;picture.SizeMode=PictureBoxSizeMode.Zoom;picture.Image=source;picture.Cursor=Cursors.Cross;
   var ok=new Button{Text="应用所选区域",Dock=DockStyle.Bottom,Height=42};ok.Click+=delegate{if(Exp.Width>10&&Level.Width>5&&step!=1){DialogResult=DialogResult.OK;Close();}else MessageBox.Show("请依次框选经验和等级两个区域。");};
   picture.MouseDown+=delegate(object s,MouseEventArgs e){origin=ToImage(e.Location);dragging=true;};
   picture.MouseMove+=delegate(object s,MouseEventArgs e){if(!dragging)return;Point p=ToImage(e.Location);selection=Rectangle.FromLTRB(Math.Min(origin.X,p.X),Math.Min(origin.Y,p.Y),Math.Max(origin.X,p.X),Math.Max(origin.Y,p.Y));picture.Invalidate();};
   picture.MouseUp+=delegate{if(!dragging)return;dragging=false;if(selection.Width<5||selection.Height<5)return;if(step==0){Exp=selection;step=1;tip.Text="现在框选等级数字（可包含 LV.）。不要包含职业或角色名。";}else{Level=selection;step=0;tip.Text="两个区域已选好，点击应用。再次拖动可重新选择。";}selection=Rectangle.Empty;picture.Invalidate();};
   picture.Paint+=delegate(object s,PaintEventArgs e){DrawBox(e.Graphics,Exp,Color.Aquamarine);DrawBox(e.Graphics,Level,Color.Gold);if(dragging)DrawBox(e.Graphics,selection,Color.White);};
   Controls.Add(picture);Controls.Add(tip);Controls.Add(ok);FormClosed+=delegate{source.Dispose();};
  }
  RectangleF View(){float scale=Math.Min((float)picture.Width/source.Width,(float)picture.Height/source.Height);return new RectangleF((picture.Width-source.Width*scale)/2,(picture.Height-source.Height*scale)/2,source.Width*scale,source.Height*scale);}
  Point ToImage(Point p){var v=View();return new Point(Math.Max(0,Math.Min(source.Width-1,(int)((p.X-v.X)*source.Width/v.Width))),Math.Max(0,Math.Min(source.Height-1,(int)((p.Y-v.Y)*source.Height/v.Height))));}
  void DrawBox(Graphics g,Rectangle r,Color c){if(r.IsEmpty)return;var v=View();float sc=v.Width/source.Width;using(var p=new Pen(c,2))g.DrawRectangle(p,v.X+r.X*sc,v.Y+r.Y*sc,r.Width*sc,r.Height*sc);}
 }
}
