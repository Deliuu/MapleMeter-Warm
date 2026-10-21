using System;
using System.IO;
using System.Windows.Media;
namespace MapleWarm {
 public static class Theme {
  public static string Id="warm";
  public static bool Warm{get{return Id=="warm";}}
  public static bool Dark{get{return Id=="mint";}}
  public static string Name{get{return Dark?"静夜薄荷":Warm?"枫糖暖光":"晴空轻白";}}
  public static string Color(string value){
   if(Warm)return value.ToUpperInvariant()=="#90715B"?"#76563F":value;
   switch(value.ToUpperInvariant()){
    case "#4A210F":return Dark?"#E5EDF9":"#10233F";
    case "#90715B":return Dark?"#B6C5DB":"#4C6485";
    case "#EED8BC":return Dark?"#2D3C50":"#DAE5F2";
    case "#226E36":return Dark?"#89EDB5":"#147BF3";
    case "#FFFDF7":case "#FFF9EF":return Dark?"#141E2A":"#F7FBFF";
    case "#FFFDF6":case "#FFFCF6":case "#FFFCF8":case "#FFFBF5":return Dark?"#18232F":"#FFFFFF";
    case "#FCF5E8":return Dark?"#16212D":"#F8FBFF";
    case "#FFFCF5":return Dark?"#233143":"#FFFFFF";
    case "#FCF4E7":return Dark?"#1C2939":"#F2F7FE";
    case "#F37750":return Dark?"#B9585C":"#258CFF";
    case "#DE5834":return Dark?"#994247":"#1476EC";
    case "#E56543":return Dark?"#CE7073":"#2585F3";
    case "#EB7848":case "#EE8553":case "#D87819":return Dark?"#85EAB2":"#197EF4";
    case "#F5E8D3":return Dark?"#2B394A":"#E2EBF6";
    case "#FFC275":return Dark?"#94EDBB":"#53AAFF";
    case "#FFA441":return Dark?"#59CA94":"#2282F6";
    case "#F8EEE1":return Dark?"#202D3D":"#EEF4FB";
    case "#FFE0BF":return Dark?"#293F58":"#DCEEFF";
    case "#A1B78B":return Dark?"#89E7B2":"#67B3FF";
    case "#8AA574":return Dark?"#54BB8A":"#3292F8";
    case "#FFF0D9":return Dark?"#202F40":"#EAF5FF";
    case "#FBE7C9":return Dark?"#182431":"#F5FAFF";
    default:return value;
   }
  }
  public static void Load(){try{var id=File.ReadAllText(Path.Combine(MapleMeter.Storage.Folder,"theme.txt")).Trim();if(id=="warm"||id=="mint"||id=="sky")Id=id;}catch{}}
  public static void Set(string id,bool save=true){if(id!="warm"&&id!="mint"&&id!="sky")throw new ArgumentException("Unknown theme");if(save){Directory.CreateDirectory(MapleMeter.Storage.Folder);File.WriteAllText(Path.Combine(MapleMeter.Storage.Folder,"theme.txt"),id);}Id=id;App.RefreshAppearance();}
 }
}
