using Godot;
using System;
using System.IO;
using System.Threading.Tasks;
namespace Aftertide;
public partial class AftertideGame {
    private async Task RunSmoke(){try{
        int checks=Checks.Run(_sim.Rules,_geo);
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        var ray=PhysicsRayQueryParameters3D.Create(_camp+Vector3.Up*10,_camp-Vector3.Up*10,1);
        var hit=GetWorld3D().DirectSpaceState.IntersectRay(ray);
        if(!hit.ContainsKey("position")||Mathf.Abs(hit["position"].AsVector3().Y-_camp.Y)>.25f)throw new InvalidOperationException("Local terrain collider mismatch");
        if(_resources.Count<100||_cove.DistanceTo(_camp)<80)throw new InvalidOperationException("Missing real gathering/expedition content");
        _sim.ChooseCamp(_camp.X,_camp.Y,_camp.Z);Save();if(!File.Exists(SavePath()))throw new InvalidOperationException("Runtime save missing");await LoadWorld();
        GD.Print($"AFTERTIDE_SMOKE_PASS domain_checks={checks} objectives=10 geography=true terrain_collision=true resources={_resources.Count} actual_save_load=true; NOT a manual playthrough or GPU performance acceptance");
        if(DisplayServer.GetName()!="headless"){
            _player.Pitch=0;_player.Camera.Rotation=Vector3.Zero;
            RefreshHud();await ToSignal(GetTree().CreateTimer(.5),SceneTreeTimer.SignalName.Timeout);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            var image=GetViewport().GetTexture().GetImage();string folder=ProjectSettings.GlobalizePath("res://outputs/aftertide-evidence");Directory.CreateDirectory(folder);image.SavePng(Path.Combine(folder,"starting-coast.png"));}
        GetTree().Quit(0);
    }catch(Exception ex){GD.PushError("AFTERTIDE_SMOKE_FAIL "+ex);GetTree().Quit(1);}}
}
