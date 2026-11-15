using System;
using System.Linq;
using System.Text.Json;
namespace Aftertide;
public sealed class Country {public string Name{get;set;}="";public string Iso{get;set;}="";public double[][][] Rings{get;set;}=Array.Empty<double[][]>();}
public sealed class StartLocation {public string Name{get;set;}="";public double Longitude{get;set;}public double Latitude{get;set;}}
public sealed class Geography {
    public StartLocation Start{get;set;}=new();public Country[] Countries{get;set;}=Array.Empty<Country>();
    public string SourceRevision{get;set;}="";public string SourceSha256{get;set;}="";
    public const double EarthRadius=6371008.8;
    private static double Rad(double x)=>x*Math.PI/180;
    public static Geography Parse(string json)=>JsonSerializer.Deserialize<Geography>(json,Rules.Json)??throw new FormatException("Missing geography");
    public (double X,double Z) Local(double longitude,double latitude)=>
        (EarthRadius*Rad(longitude-Start.Longitude)*Math.Cos(Rad(Start.Latitude)),-EarthRadius*Rad(latitude-Start.Latitude));
    public (double Longitude,double Latitude) Geographic(double x,double z)=>
        (Start.Longitude+x/(EarthRadius*Math.Cos(Rad(Start.Latitude)))*180/Math.PI,Start.Latitude-z/EarthRadius*180/Math.PI);
    public static (double X,double Z) Overview(double lon,double lat)=>(EarthRadius*Rad(lon+75)*Math.Cos(Rad(18)),-EarthRadius*Rad(lat-18));
    public static double Distance(double lonA,double latA,double lonB,double latB){double dlat=Rad(latB-latA),dlon=Rad(lonB-lonA);
        double a=Math.Pow(Math.Sin(dlat/2),2)+Math.Cos(Rad(latA))*Math.Cos(Rad(latB))*Math.Pow(Math.Sin(dlon/2),2);
        return 2*EarthRadius*Math.Asin(Math.Min(1,Math.Sqrt(a)));}
    public bool Land(double x,double z){var p=Geographic(x,z);return Countries.Single(c=>c.Iso=="JAM").Rings.Any(r=>Inside(r,p.Longitude,p.Latitude));}
    public static bool Inside(double[][] ring,double x,double y){bool inside=false;for(int i=0,j=ring.Length-1;i<ring.Length;j=i++){
        var a=ring[i];var b=ring[j];if((a[1]>y)!=(b[1]>y)&&x<(b[0]-a[0])*(y-a[1])/(b[1]-a[1])+a[0])inside=!inside;}return inside;}
    public double CoastDistance(double x,double z){double best=double.PositiveInfinity;
        foreach(var ring in Countries.Single(c=>c.Iso=="JAM").Rings)for(int i=0;i<ring.Length;i++){
            var a=Local(ring[i][0],ring[i][1]);var b=Local(ring[(i+1)%ring.Length][0],ring[(i+1)%ring.Length][1]);
            double dx=b.X-a.X,dz=b.Z-a.Z,t=Math.Clamp(((x-a.X)*dx+(z-a.Z)*dz)/(dx*dx+dz*dz+1e-12),0,1);
            best=Math.Min(best,Math.Sqrt(Math.Pow(x-a.X-dx*t,2)+Math.Pow(z-a.Z-dz*t,2)));}return best;}
    public float Height(float x,float z){double d=CoastDistance(x,z);if(!Land(x,z))return (float)-Math.Min(18,.05+d*.09);
        double detail=(Math.Sin(x*.017)*Math.Cos(z*.021)+.35*Math.Sin(z*.047+x*.013))*Math.Min(1,d/90);
        return (float)(Math.Min(11,d*.055)+detail);}
}
