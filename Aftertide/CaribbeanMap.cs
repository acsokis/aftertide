using Godot;
using System;
namespace Aftertide;
public partial class CaribbeanMap : Control {
    public Geography Geography{get;set;}=null!;
    public override void _Draw(){if(Geography==null)return;
        DrawRect(new Rect2(Vector2.Zero,Size),new Color("102b40"));
        Vector2 Map(double lon,double lat){var p=Aftertide.Geography.Overview(lon,lat);var min=Aftertide.Geography.Overview(-91,29);var max=Aftertide.Geography.Overview(-58,7);
            float scale=Math.Min(Size.X/(float)(max.X-min.X),Size.Y/(float)(max.Z-min.Z));
            return new Vector2((float)(p.X-min.X)*scale,(float)(p.Z-min.Z)*scale);}
        foreach(var country in Geography.Countries)foreach(var ring in country.Rings){
            var points=new Vector2[ring.Length+1];for(int i=0;i<ring.Length;i++)points[i]=Map(ring[i][0],ring[i][1]);points[^1]=points[0];
            DrawPolyline(points,new Color("8fc99a"),1.2f,true);}
        foreach(var p in new[]{("CUBA",-79.7,22.1),("HISPANIOLA",-71.5,19.0),("JAMAICA",-77.3,17.3),
            ("PUERTO RICO",-66.3,18.1),("BAHAMAS",-76.5,25.0),("CAYMAN",-81.3,19.8),("LESSER ANTILLES",-62.0,14.2)})
            DrawString(ThemeDB.FallbackFont,Map(p.Item2,p.Item3),p.Item1,HorizontalAlignment.Left,-1,13,new Color("ecdfad"));
        var start=Map(Geography.Start.Longitude,Geography.Start.Latitude);DrawCircle(start,5,Colors.OrangeRed);
        DrawString(ThemeDB.FallbackFont,start+new Vector2(9,18),"YOUR CAMP — detailed local area",fontSize:13,modulate:Colors.White);
    }
    public override void _Notification(int what){if(what==NotificationResized)QueueRedraw();}
}
