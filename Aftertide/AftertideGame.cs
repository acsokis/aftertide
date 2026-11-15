using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace Aftertide;

/// <summary>AFTERTIDE's original playable loop; no legacy quests/towns are instantiated.</summary>
public partial class AftertideGame : Node3D {
    private Simulation _sim=null!;private Geography _geo=null!;private Player _player=null!;
    private CharacterBody3D _worker=null!;private NavigationAgent3D _agent=null!;
    private Node3D _content=null!,_boat=null!;private Vector3 _camp,_cove,_dock;
    private Label _hud=null!,_objective=null!,_hint=null!,_notice=null!;private PanelContainer _panel=null!;
    private VBoxContainer _menu=null!;private Control _ui=null!;private Node3D _ghost=null!;
    private ShaderMaterial _water=null!;private DirectionalLight3D _sun=null!;private Godot.Environment _environment=null!;
    private bool _ready,_sailing;private string _placing="";private Vector3 _placement;
    private readonly Dictionary<ulong,(string Id,string Item,int Count)> _resources=new();
    private readonly Dictionary<string,Node3D> _resourceNodes=new();private readonly List<Node3D> _buildings=new();
    private double _autosave,_refresh;private Vector3 _lastBoat;private string _status="Loading geography…";
    private NavigationRegion3D? _region;private bool _navDirty,_navBaking;
    private StaticBody3D? _boatBody;private bool _loading;
    private readonly List<Vector3> _treePositions=new();private GpuParticles3D? _rain;
    private readonly Dictionary<string,Material> _materials=new();private Vector3[] _terrainFaces=Array.Empty<Vector3>();
    public override async void _Ready(){
        try{
            System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.InvariantCulture;
            GetNodeOrNull<Node>("/root/LoadingScreen")?.Call("HideLoading");
            BuildHud();_notice.Text="Loading geography: reading verified coastlines…";
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            _sim=new Simulation(Rules.Parse(Godot.FileAccess.GetFileAsString("res://Aftertide/Data/gameplay.json")));
            _geo=Geography.Parse(Godot.FileAccess.GetFileAsString("res://Aftertide/Data/geography.json"));
            _notice.Text="Loading local coast: generating terrain and collision…";
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            BuildEnvironment();BuildTerrain();FindSites();
            _content=new Node3D{Name="PersistentWorldContent"};AddChild(_content);
            CreateResources();CreateMarkers();
            _player=new Player{Simulation=_sim,Position=Ground(_camp+new Vector3(0,0,6))+Vector3.Up*.2f};AddChild(_player);
            _player.LookAt(new Vector3(_camp.X,_player.Position.Y,_camp.Z));BuildWorker();
            _notice.Text="Loading navigation: baking the local walking region…";
            await BuildNavigation();
            _ghost=Mesh(new BoxMesh{Size=new Vector3(2,.15f,2)},Colors.LightGreen,Vector3.Zero);_ghost.Visible=false;
            _ready=true;_notice.Text="AFTERTIDE — Survive the islands. Build a future.";
            GD.Print("AFTERTIDE_WORLD_READY countries="+_geo.Countries.Length+" resources="+_resources.Count+" geography="+_geo.SourceRevision);
            if(System.Environment.GetEnvironmentVariable("AFTERTIDE_SMOKE")=="1")await RunSmoke();
        }catch(Exception ex){GD.PushError("AFTERTIDE_BOOT_FAIL "+ex);if(_notice!=null)_notice.Text="Loading failed: "+ex.Message+" — check the game log.";Input.MouseMode=Input.MouseModeEnum.Visible;
            if(System.Environment.GetEnvironmentVariable("AFTERTIDE_SMOKE")=="1")GetTree().Quit(1);}
    }
    private Material Mat(string hex){if(!_materials.TryGetValue(hex,out var m)){m=new StandardMaterial3D{AlbedoColor=new Color(hex),Roughness=.85f};_materials[hex]=m;}return m;}
    private Node3D Mesh(Mesh mesh,Color color,Vector3 at,Node? parent=null){var n=new MeshInstance3D{Mesh=mesh,MaterialOverride=Mat(color.ToHtml()),Position=at,CastShadow=GeometryInstance3D.ShadowCastingSetting.On};(parent??this).AddChild(n);return n;}
    private Node3D Box(Vector3 at,Vector3 size,string color,Node? parent=null)=>Mesh(new BoxMesh{Size=size},new Color(color),at,parent);
    private StaticBody3D Solid(Vector3 at,Vector3 size,Node? parent=null){var b=new StaticBody3D{Position=at,CollisionLayer=1,CollisionMask=0};(parent??_content).AddChild(b);b.AddChild(new CollisionShape3D{Shape=new BoxShape3D{Size=size}});return b;}
    private Vector3 Ground(Vector3 at)=>new(at.X,_geo.Height(at.X,at.Z),at.Z);
    private void BuildEnvironment(){
        _environment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Sky,
            Sky=new Sky{SkyMaterial=new ProceduralSkyMaterial{SkyTopColor=new Color("277cae"),SkyHorizonColor=new Color("b6dbe1"),GroundHorizonColor=new Color("b6dbe1"),GroundBottomColor=new Color("496955")}},
            AmbientLightSource=Godot.Environment.AmbientSource.Sky,AmbientLightEnergy=.65f,
            ReflectedLightSource=Godot.Environment.ReflectionSource.Sky,TonemapMode=Godot.Environment.ToneMapper.Filmic,
            FogEnabled=true,FogDensity=.0006f,FogLightColor=new Color("b0cfce")};
        AddChild(new WorldEnvironment{Environment=_environment});
        _sun=new DirectionalLight3D{RotationDegrees=new Vector3(-48,-32,0),ShadowEnabled=true,LightEnergy=1.2f,DirectionalShadowMaxDistance=160};AddChild(_sun);
        _water=new ShaderMaterial{Shader=GD.Load<Shader>("res://Aftertide/Water.gdshader")};
        AddChild(new MeshInstance3D{Mesh=new PlaneMesh{Size=new Vector2(5000,5000),SubdivideWidth=160,SubdivideDepth=160},MaterialOverride=_water,Position=Vector3.Zero});
        // Original bounded stylized cloud geometry, not imported commercial assets.
        for(int i=0;i<24;i++){var cloud=new Node3D{Position=new Vector3(-600+i*59,150+(i%4)*12,-350+(i*83)%700)};AddChild(cloud);
            for(int j=0;j<3;j++)Mesh(new SphereMesh{Radius=22+j*4,Height=17+j*3,RadialSegments=12,Rings=6},new Color("eef5ef"),new Vector3(j*17,0,(j%2)*8),cloud);}
        if(DisplayServer.GetName()!="headless"){
            _rain=new GpuParticles3D{Amount=650,Lifetime=1.7,LocalCoords=false,Emitting=false,
                ProcessMaterial=new ParticleProcessMaterial{EmissionShape=ParticleProcessMaterial.EmissionShapeEnum.Box,
                    EmissionBoxExtents=new Vector3(18,1,18),Direction=Vector3.Down,Spread=7,
                    InitialVelocityMin=17,InitialVelocityMax=21,Gravity=new Vector3(2,-12,0)},
                DrawPass1=new QuadMesh{Size=new Vector2(.018f,.4f)},CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};
            _rain.MaterialOverride=new StandardMaterial3D{AlbedoColor=new Color(.68f,.84f,.9f,.5f),Transparency=BaseMaterial3D.TransparencyEnum.Alpha,
                BillboardMode=BaseMaterial3D.BillboardModeEnum.Enabled,ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded};AddChild(_rain);}
    }
    private void BuildTerrain(){
        const int n=128;const float step=8,half=512;var vertices=new List<Vector3>(n*n*6);var st=new SurfaceTool();st.Begin(Godot.Mesh.PrimitiveType.Triangles);
        void V(float x,float z){var p=new Vector3(x,_geo.Height(x,z),z);vertices.Add(p);st.SetColor(p.Y<1.6f?new Color("dacb96"):new Color("589a65"));st.AddVertex(p);}
        for(int z=0;z<n;z++)for(int x=0;x<n;x++){float a=-half+x*step,b=-half+z*step;
            V(a,b);V(a+step,b);V(a+step,b+step);V(a,b);V(a+step,b+step);V(a,b+step);}
        st.GenerateNormals(true);var mesh=st.Commit();AddChild(new MeshInstance3D{Mesh=mesh,MaterialOverride=new StandardMaterial3D{VertexColorUseAsAlbedo=true,Roughness=.96f,CullMode=BaseMaterial3D.CullModeEnum.Disabled}});
        _terrainFaces=vertices.ToArray();var body=new StaticBody3D{CollisionLayer=1,CollisionMask=0};AddChild(body);body.AddChild(new CollisionShape3D{Shape=new ConcavePolygonShape3D{Data=_terrainFaces,BackfaceCollision=true}});
    }
    private void FindSites(){
        _camp=Vector3.Zero;float best=float.MaxValue;
        for(int z=-100;z<=100;z+=4)for(int x=-100;x<=100;x+=4){float y=_geo.Height(x,z);if(y<1.8f||y>5.5f)continue;
            float score=x*x+z*z;if(score<best){best=score;_camp=new Vector3(x,y,z);}}
        if(best==float.MaxValue)throw new InvalidOperationException("No supported cartographic starting area.");
        best=float.MaxValue;
        for(int z=-130;z<=130;z+=3)for(int x=-130;x<=130;x+=3){float h=_geo.Height(x,z);if(h>-.3f||h< -2)continue;
            float d=new Vector2(x-_camp.X,z-_camp.Z).LengthSquared();if(d<best){best=d;_dock=new Vector3(x,.15f,z);}}
        _cove=_camp;
        for(int z=-220;z<=220;z+=5)for(int x=-220;x<=220;x+=5){float y=_geo.Height(x,z);var p=new Vector3(x,y,z);
            if(y>.7f&&y<2.5f&&p.DistanceTo(_camp)>115&&p.DistanceTo(_camp)<150){_cove=p;break;}}
        if(_cove.DistanceTo(_camp)<80)throw new InvalidOperationException("No coastal expedition site found.");
    }
    private void Label3(string text,Vector3 position,Node? parent=null){(parent??_content).AddChild(new Label3D{Text=text,Position=position,FontSize=30,PixelSize=.012f,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,Modulate=new Color("ffe4a8")});}
    private StaticBody3D Marker(string id,string text,Vector3 p,string color){var b=new StaticBody3D{Position=p,CollisionLayer=2,CollisionMask=0};_content.AddChild(b);b.SetMeta("interact",id);
        b.AddChild(new CollisionShape3D{Position=Vector3.Up,Shape=new BoxShape3D{Size=new Vector3(1,2,1)}});
        Box(Vector3.Up,new Vector3(.15f,2,.15f),color,b);Box(new Vector3(.4f,1.7f,0),new Vector3(.8f,.45f,.08f),color,b);Label3(text,Vector3.Up*2.7f,b);return b;}
    private void CreateMarkers(){Marker("camp","CAMP SITE · E",_camp,"e4b85a");Marker("dock","BOAT SITE",_dock,"68aeb8");
        Marker("cove","SUPPLY COVE",_cove,"eea35b");
        Marker("spring","FRESHWATER SPRING · E",Ground(_camp+new Vector3(-10,0,-9)),"7cd3df");}
    private void CreateResources(){
        var rng=new Random(171511);
        foreach(var spec in new[]{("wood",42,3,"865e3d"),("stone",20,3,"9baba5"),("fiber",20,4,"86b85a"),("herb",10,3,"b3d575"),("raw_food",18,3,"61b4c6")}){
            for(int i=0;i<spec.Item2;i++){
                Vector3 p=Vector3.Zero;bool found=false;
                for(int trial=0;trial<300;trial++){p=Ground(_camp+new Vector3((float)(rng.NextDouble()-.5)*150,0,(float)(rng.NextDouble()-.5)*150));
                    if(p.Y>.5f&&p.DistanceTo(_camp)>9){found=true;break;}}
                if(!found)continue;string id="aftertide:resource:"+spec.Item1+":"+i;
                var body=new StaticBody3D{Position=p,CollisionLayer=2,CollisionMask=0};_content.AddChild(body);
                body.AddChild(new CollisionShape3D{Position=Vector3.Up*.4f,Shape=new SphereShape3D{Radius=.45f}});
                Mesh(spec.Item1=="stone"?new SphereMesh{Radius=.45f,Height=.7f,RadialSegments=8,Rings=4}:
                    new BoxMesh{Size=new Vector3(.7f,.6f,.5f)},new Color(spec.Item4),Vector3.Up*.35f,body);
                _resources[body.GetInstanceId()]=(id,spec.Item1,spec.Item3);_resourceNodes[id]=body;
            }
        }
        for(int i=0;i<80;i++){var p=Ground(_camp+new Vector3((float)(rng.NextDouble()-.5)*700,0,(float)(rng.NextDouble()-.5)*700));if(p.Y<3||p.DistanceTo(_camp)<30||p.DistanceTo(_cove)<15)continue;
            var tree=new Node3D{Position=p};_content.AddChild(tree);
            _treePositions.Add(p);
            Mesh(new CylinderMesh{TopRadius=.16f,BottomRadius=.34f,Height=6,RadialSegments=7},new Color("8b6746"),Vector3.Up*3,tree);
            for(int j=0;j<7;j++){var leaf=Box(new Vector3(0,6,0),new Vector3(.8f,.16f,4.0f),"46915d",tree);leaf.Rotation=new Vector3(.24f,j*Mathf.Tau/7,.1f);leaf.Position+=leaf.Basis.Z*1.4f;}
            var b=new StaticBody3D{CollisionLayer=2,CollisionMask=0};tree.AddChild(b);b.AddChild(new CollisionShape3D{Position=Vector3.Up*3,Shape=new CylinderShape3D{Radius=.34f,Height=6}});
        }
    }
    private void BuildWorker(){_worker=new CharacterBody3D{Position=Ground(_camp+new Vector3(4,0,3)),CollisionLayer=8,CollisionMask=7,FloorSnapLength=.5f};AddChild(_worker);
        _worker.AddChild(new CollisionShape3D{Position=Vector3.Up*.85f,Shape=new CapsuleShape3D{Radius=.32f,Height=1.7f}});
        Mesh(new CapsuleMesh{Radius=.25f,Height=1.1f},new Color("598f91"),Vector3.Up*.9f,_worker);
        Mesh(new SphereMesh{Radius=.18f,Height=.36f},new Color("ad805f"),Vector3.Up*1.6f,_worker);
        Label3("MARA · worker",Vector3.Up*2.1f,_worker);_worker.SetMeta("interact","worker");
        _agent=new NavigationAgent3D{PathDesiredDistance=.4f,TargetDesiredDistance=.7f,Radius=.4f,Height=1.7f,AvoidanceEnabled=true,MaxSpeed=1.8f,AvoidancePriority=.5f};_worker.AddChild(_agent);
        _agent.VelocityComputed+=safe=>{if(!_ready||_loading)return;float dt=(float)GetPhysicsProcessDeltaTime();
            float y=_worker.IsOnFloor()?-.2f:_worker.Velocity.Y-18*dt;_worker.Velocity=new Vector3(safe.X,y,safe.Z);_worker.MoveAndSlide();};
    }
    private async Task BuildNavigation(){if(_navBaking)return;_navBaking=true;_navDirty=false;
        try{var nav=new NavigationMesh{AgentRadius=.5f,AgentHeight=1.8f,AgentMaxClimb=.5f,AgentMaxSlope=35,CellSize=.25f,CellHeight=.25f,
            FilterBakingAabb=new Aabb(new Vector3(_camp.X-95,.1f,_camp.Z-95),new Vector3(190,35,190))};
        var data=new NavigationMeshSourceGeometryData3D();data.AddFaces(_terrainFaces,Transform3D.Identity);
        foreach(var s in _sim.State.Structures.Where(s=>s.Kind is not ("boat" or "shelter")))
            data.AddProjectedObstruction(new[]{new Vector3(s.X-.85f,s.Y,s.Z-.75f),new Vector3(s.X+.85f,s.Y,s.Z-.75f),
                new Vector3(s.X+.85f,s.Y,s.Z+.75f),new Vector3(s.X-.85f,s.Y,s.Z+.75f)},s.Y,1.4f,true);
        foreach(var p in _treePositions.Concat(_resourceNodes.Values.Where(GodotObject.IsInstanceValid).Select(n=>n.GlobalPosition))){
            if(p.DistanceTo(_camp)>100)continue;float r=.55f;
            data.AddProjectedObstruction(new[]{p+new Vector3(-r,0,-r),p+new Vector3(r,0,-r),p+new Vector3(r,0,r),p+new Vector3(-r,0,r)},p.Y,2,true);}
        if(_region==null){_region=new NavigationRegion3D();AddChild(_region);}
        var completion=new TaskCompletionSource();NavigationServer3D.BakeFromSourceGeometryDataAsync(nav,data,Callable.From(()=>completion.SetResult()));
        await completion.Task;_region.NavigationMesh=nav;await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        if(nav.GetPolygonCount()==0)throw new InvalidOperationException("Walking region is empty.");
        }finally{_navBaking=false;}
    }
    public override void _Process(double delta){if(!_ready||_loading)return;float dt=(float)Math.Min(delta,.1);
        if(_navDirty&&!_navBaking)_=RefreshNavigation();
        _sim.Sheltered=_sim.State.Structures.Any(s=>s.Kind=="shelter"&&_player.Position.DistanceTo(new Vector3(s.X,s.Y,s.Z))<4);
        _sim.Advance(dt);_water.SetShaderParameter("storm",_sim.Storm?1f:0f);
        float daylight=Mathf.Clamp(Mathf.Sin((float)(_sim.Hour-6)/24*Mathf.Tau),.05f,1);
        _sun.RotationDegrees=new Vector3((float)(-90+(_sim.Hour-12)*12),-32,0);_sun.LightEnergy=daylight*(_sim.Storm?.4f:1.2f);
        if(_rain!=null){_rain.GlobalPosition=_player.GlobalPosition+Vector3.Up*13;_rain.Emitting=_sim.Rain;}
        _sun.LightColor=_sim.Hour>18?new Color("8db0e4"):new Color("fff0cd");
        if(_sailing){Vector2 move=Input.GetVector("at_left","at_right","at_forward","at_back");if(!_player.MenuOpen){_boat.Rotation=new Vector3(0,_boat.Rotation.Y-move.X*dt*.8f,0);
                var target=_boat.Position-_boat.Basis.Z*(-move.Y)*dt*5f;
                if(Mathf.Abs(target.X)<470&&Mathf.Abs(target.Z)<470&&_geo.Height(target.X,target.Z)<-.15f)_boat.Position=target;}
            _player.Position=_boat.Position+Vector3.Up*.8f;_sim.State.BoatDistance+=new Vector2(_boat.Position.X-_lastBoat.X,_boat.Position.Z-_lastBoat.Z).Length();_lastBoat=_boat.Position;}
        if(!_sailing&&_player.Position.DistanceTo(_cove)<9)_sim.State.RemoteVisited=true;
        if(_sim.State.Health<=0||_player.Position.Y< -6){_sim.Recover();_player.Position=Ground(_camp+Vector3.Right*3)+Vector3.Up*.2f;_player.Velocity=Vector3.Zero;_sailing=false;_player.Sailing=false;}
        if(_placing!="")UpdatePlacement();
        _refresh+=delta;if(_refresh>.15){_refresh=0;RefreshHud();}
        _autosave+=delta;if(_autosave>60){_autosave=0;Save();}
    }
    private async Task RefreshNavigation(){try{await BuildNavigation();}catch(Exception e){_status="Navigation update failed: "+e.Message;GD.PushError(_status);}}
    public override void _PhysicsProcess(double delta){if(!_ready||_loading)return;
        Vector3 target=_camp+Vector3.Right*4;var job=_sim.Rules.Production.FirstOrDefault(p=>p.Id==_sim.State.WorkerJob);
        if(job!=null){var s=_sim.State.Structures.FirstOrDefault(s=>s.Kind==job.Building);if(s!=null)target=new Vector3(s.X,s.Y,s.Z)+Vector3.Right*2.1f;}
        target=Ground(target);
        if(_agent.TargetPosition.DistanceTo(target)>.3f)_agent.TargetPosition=target;
        _sim.WorkerReady=!_navBaking&&_worker.Position.DistanceTo(target)<1.2f;
        var velocity=_worker.Velocity;velocity.Y=_worker.IsOnFloor()?-.2f:velocity.Y-18*(float)delta;
        if(!_sim.WorkerReady&&!_agent.IsNavigationFinished()){var next=_agent.GetNextPathPosition();var d=next-_worker.Position;d.Y=0;d=d.Normalized();velocity.X=d.X*1.8f;velocity.Z=d.Z*1.8f;if(d.LengthSquared()>.01f)_worker.Rotation=new Vector3(0,Mathf.Atan2(d.X,d.Z),0);}
        else{velocity.X=0;velocity.Z=0;}_agent.Velocity=new Vector3(velocity.X,0,velocity.Z);
    }
    private Godot.Collections.Dictionary Ray(float length=4){var from=_player.Camera.GlobalPosition;return GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(from,from-_player.Camera.GlobalBasis.Z*length,3));}
    public override void _UnhandledInput(InputEvent e){if(!_ready)return;
        if(e is InputEventKey key&&key.Pressed&&!key.Echo){switch(key.PhysicalKeycode){
            case Key.Escape:if(_placing!=""){_placing="";_ghost.Visible=false;}else ToggleMenu("camp");break;
            case Key.E:if(!_player.MenuOpen){if(_placing!="")Place();else Interact();}break;
            case Key.B:ToggleMenu("build");break;case Key.C:ToggleMenu("craft");break;case Key.I:ToggleMenu("inventory");break;case Key.M:ToggleMenu("map");break;
            case Key.Key1:_sim.Consume("water");break;case Key.Key2:_sim.Consume("food");break;case Key.Key3:_sim.Consume("bandage");break;
            case Key.F5:Save();break;case Key.F9:Load();break;}}
    }
    private void Interact(){if(_sailing){Dismount();return;}var hit=Ray();if(!hit.ContainsKey("collider"))return;
        var body=hit["collider"].AsGodotObject() as Node;if(body==null)return;
        if(_resources.TryGetValue(body.GetInstanceId(),out var resource)){if(_sim.Collect(resource.Id,resource.Item,resource.Count)){_resourceNodes[resource.Id].QueueFree();_resourceNodes.Remove(resource.Id);_navDirty=true;}_status=_sim.Notice;return;}
        string id=body.HasMeta("interact")?body.GetMeta("interact").AsString():"";
        switch(id){case "camp":_sim.ChooseCamp(_camp.X,_camp.Y,_camp.Z);ToggleMenu("camp");break;
            case "spring":_sim.State.Backpack.Add("raw_water",3,_sim.State.Clock,_sim.Rules);_status="Collected untreated freshwater. Treat it before drinking.";break;
            case "worker":ToggleMenu("work");break;case "boat":_sailing=true;_player.Sailing=true;_lastBoat=_boat.Position;if(_boatBody!=null)_player.AddCollisionExceptionWith(_boatBody);break;
            case "cove":_sim.State.RemoteVisited=true;_status=_sim.Collect("aftertide:cove:cargo","cargo",8)?"Recovered expedition cargo. Return it to camp.":_sim.Notice;break;
            default:if(id.StartsWith("building:"))ToggleMenu("camp");break;}
    }
    private void Dismount(){for(int radius=3;radius<=14;radius++)for(int i=0;i<48;i++){float a=i*Mathf.Tau/48;var p=Ground(_boat.Position+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius);
            if(p.Y<.4f)continue;var query=new PhysicsShapeQueryParameters3D{Shape=new CapsuleShape3D{Radius=.34f,Height=1.8f},CollisionMask=3,
                Transform=new Transform3D(Basis.Identity,p+Vector3.Up*.94f)};
            if(GetWorld3D().DirectSpaceState.IntersectShape(query,1).Count>0)continue;
            if(_player.TestMove(_player.GlobalTransform,p+Vector3.Up*.08f-_player.GlobalPosition))continue;
            _sailing=false;_player.Sailing=false;_player.Position=p+Vector3.Up*.08f;_player.Velocity=Vector3.Zero;
            if(_boatBody!=null)_player.RemoveCollisionExceptionWith(_boatBody);return;}_status="Move closer to a clear shore before landing.";}
    private bool PlacementValid(string kind,Vector3 p){if(kind=="boat")return p.DistanceTo(_dock)<12&&_geo.Height(p.X,p.Z)<-.15f;
        if(p.DistanceTo(_camp)>45||p.Y<.5f)return false;float radius=(float)_sim.Rules.Buildings.Single(b=>b.Id==kind).Radius;
        foreach(var d in new[]{new Vector3(radius,0,0),new Vector3(-radius,0,0),new Vector3(0,0,radius),new Vector3(0,0,-radius)})
            if(Mathf.Abs(_geo.Height(p.X+d.X,p.Z+d.Z)-p.Y)>.8f)return false;
        if(_sim.State.Structures.Any(s=>new Vector2(p.X-s.X,p.Z-s.Z).Length()<radius+1.8f))return false;
        var q=new PhysicsShapeQueryParameters3D{Shape=new BoxShape3D{Size=new Vector3(radius*1.6f,1.3f,radius*1.6f)},
            Transform=new Transform3D(Basis.Identity,p+Vector3.Up*.8f),CollisionMask=15};return GetWorld3D().DirectSpaceState.IntersectShape(q,1).Count==0;
    }
    private void UpdatePlacement(){var from=_player.Camera.GlobalPosition;var dir=-_player.Camera.GlobalBasis.Z;var hit=Ray(12);
        if(_placing=="boat"&&Mathf.Abs(dir.Y)>.01f){float t=-from.Y/dir.Y;_placement=from+dir*Mathf.Clamp(t,2,15);_placement.Y=.15f;}
        else if(hit.ContainsKey("position"))_placement=Ground(hit["position"].AsVector3());else _placement=Ground(_player.Position-_player.GlobalBasis.Z*7);
        _ghost.Position=_placement+Vector3.Up*.1f;_ghost.Visible=true;
        ((MeshInstance3D)_ghost).MaterialOverride=Mat(PlacementValid(_placing,_placement)?"84cf9c":"e18072");}
    private void Place(){if(!PlacementValid(_placing,_placement)){_status="Need supported, clear ground near camp; boats need the marked shallow-water site.";return;}
        if(_sim.Build(_placing,_placement.X,_placement.Y,_placement.Z,_sim.NextId())){CreateBuilding(_sim.State.Structures[^1]);_placing="";_ghost.Visible=false;}_status=_sim.Notice;}
    private void CreateBuilding(Structure s){var root=new Node3D{Position=new Vector3(s.X,s.Y,s.Z)};_content.AddChild(root);_buildings.Add(root);
        _navDirty=true;
        if(s.Kind=="boat"){
            _boat=root;Box(Vector3.Zero,new Vector3(1.8f,.45f,4),"906844",root);Box(Vector3.Up*.6f,new Vector3(.15f,2,.15f),"d8c49a",root);
            Box(new Vector3(.6f,1.1f,0),new Vector3(1.2f,1.3f,.08f),"e5d9af",root);
            var b=new StaticBody3D{CollisionLayer=2,CollisionMask=0};root.AddChild(b);_boatBody=b;b.SetMeta("interact","boat");b.AddChild(new CollisionShape3D{Shape=new BoxShape3D{Size=new Vector3(2,.8f,4)}});
            return;}
        if(s.Kind=="shelter"){
            foreach(float x in new[]{-1.7f,1.7f})foreach(float z in new[]{-1.7f,1.7f}){
                var p=new Vector3(x,1.3f,z);Box(p,new Vector3(.16f,2.6f,.16f),"795337",root);Solid(p,new Vector3(.16f,2.6f,.16f),root);}
            Box(Vector3.Up*2.7f,new Vector3(4,.25f,4),"52776a",root);
        }else{Box(Vector3.Up*.45f,new Vector3(1.4f,.9f,1.2f),s.Kind=="waterworks"?"588f98":"9b784e",root);
            Solid(Vector3.Up*.45f,new Vector3(1.4f,.9f,1.2f),root);}
        var interact=new StaticBody3D{Position=Vector3.Up,CollisionLayer=2,CollisionMask=0};root.AddChild(interact);interact.SetMeta("interact","building:"+s.Kind);
        interact.AddChild(new CollisionShape3D{Shape=new SphereShape3D{Radius=.6f}});Label3(_sim.Rules.Buildings.Single(b=>b.Id==s.Kind).Name,Vector3.Up*2.2f,root);
    }
    private void BuildHud(){var layer=new CanvasLayer();AddChild(layer);_ui=new Control{MouseFilter=Control.MouseFilterEnum.Ignore};layer.AddChild(_ui);_ui.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var backing=new ColorRect{Color=new Color(.025f,.09f,.11f,.55f),Position=new Vector2(12,10),Size=new Vector2(610,385),MouseFilter=Control.MouseFilterEnum.Ignore};_ui.AddChild(backing);
        Label L(string text,Vector2 at,int size){var l=new Label{Text=text,Position=at,MouseFilter=Control.MouseFilterEnum.Ignore};l.AddThemeFontSizeOverride("font_size",size);
            l.AddThemeColorOverride("font_outline_color",new Color(0,0,0,.8f));l.AddThemeConstantOverride("outline_size",3);_ui.AddChild(l);return l;}
        L("AFTERTIDE",new Vector2(22,16),30);L("Survive the islands. Build a future.",new Vector2(24,55),16);
        _hud=L("Loading…",new Vector2(24,88),16);_objective=L("",new Vector2(24,160),18);_objective.CustomMinimumSize=new Vector2(570,0);_objective.AutowrapMode=TextServer.AutowrapMode.WordSmart;
        _notice=L("Loading…",new Vector2(24,260),16);_notice.CustomMinimumSize=new Vector2(570,0);_notice.AutowrapMode=TextServer.AutowrapMode.WordSmart;
        _hint=L("",new Vector2(24,340),16);
        var cross=L("+",Vector2.Zero,22);cross.SetAnchorsPreset(Control.LayoutPreset.Center);cross.OffsetLeft=-6;cross.OffsetTop=-12;
        var help=L("WASD move · Mouse look · E interact/land · B build · C craft · I pack · M chart · 1 drink · 2 eat · 3 heal · F5 save · F9 load · Esc menu",Vector2.Zero,14);
        help.AnchorTop=1;help.AnchorBottom=1;help.OffsetTop=-36;help.OffsetLeft=22;
        _panel=new PanelContainer{Visible=false,AnchorLeft=1,AnchorRight=1,AnchorBottom=1,OffsetLeft=-490,OffsetRight=-20,OffsetTop=60,OffsetBottom=-55};_ui.AddChild(_panel);
        var scroll=new ScrollContainer();_panel.AddChild(scroll);_menu=new VBoxContainer{SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};scroll.AddChild(_menu);
    }
    private void RefreshHud(){_hud.Text=$"Health {_sim.State.Health:F0}   Water {_sim.State.Water:F0}   Food {_sim.State.Food:F0}   Stamina {_sim.State.Stamina:F0}\nPack {_sim.State.Backpack.Mass(_sim.Rules):F1}/{_sim.State.Backpack.Capacity} kg   Trust {_sim.State.Trust:F0}   Day {_sim.Day+1} {_sim.Hour:00.0}h";
        var q=_sim.CurrentQuest;_objective.Text=q==null?"CHAPTER COMPLETE — your settlement persists.":$"OBJECTIVE {_sim.State.Completed.Count+1}/10 · {q.Title}\n{q.Description}";
        _notice.Text=(_sim.Storm?"STORM — seek shelter. ":_sim.State.StormScheduled>_sim.State.Clock?$"Storm in {_sim.State.StormScheduled-_sim.State.Clock:F0}s. ":_sim.Rain?"Rain. ":"Clear. ")+_status+"\n"+_sim.Notice;
        _hint.Text=_placing!=""?"E to place "+_placing+" · Esc to cancel":_sailing?"W/S sail · A/D steer · E land near shore":"Look at resources or a marker and press E.";}
    private void ToggleMenu(string page){if(_panel.Visible&&_panel.GetMeta("page","").AsString()==page){CloseMenu();return;}
        foreach(var child in _menu.GetChildren()){_menu.RemoveChild(child);child.QueueFree();}
        _panel.SetMeta("page",page);_panel.Visible=true;_player.MenuOpen=true;Input.MouseMode=Input.MouseModeEnum.Visible;
        void Text(string s){_menu.AddChild(new Label{Text=s,AutowrapMode=TextServer.AutowrapMode.WordSmart,CustomMinimumSize=new Vector2(430,0)});}
        void Button(string s,Action a){var b=new Button{Text=s,SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};b.Pressed+=()=>{a();RefreshHud();};_menu.AddChild(b);}
        Button("Resume",CloseMenu);
        if(page=="map"){
            Text("CARIBBEAN CHART · true geographic proportions\nNatural Earth, public domain, 1:10 million generalization. Only the Jamaica starting coast is loaded in detailed 3D. Relief is authored, not a surveyed DEM.");
            _menu.AddChild(new CaribbeanMap{Geography=_geo,CustomMinimumSize=new Vector2(430,330)});return;}
        if(page=="build"){Text("BUILD · materials are taken from your backpack. Aim at clear ground, then E. Boat: shallow water near the marked site.");
            foreach(var b in _sim.Rules.Buildings)Button(b.Name+" · "+Cost(b.Inputs),()=>{_placing=b.Id;CloseMenu();});return;}
        if(page=="craft"){Text("CRAFT · original balance first-playable-1. Nearby work sites are required for food/water.");foreach(var r in _sim.Rules.Recipes)Button(r.Name+" · "+Cost(r.Inputs),()=>{
                var building=_sim.State.Structures.FirstOrDefault(s=>s.Kind==r.Requires);if(r.Requires!=""&&(building==null||_player.Position.DistanceTo(new Vector3(building.X,building.Y,building.Z))>6)){_status="Move within six metres of the required work site.";return;}
                _sim.Craft(r.Id,_sim.NextId());ToggleMenu("inventory");});return;}
        if(page=="inventory"){Text("BACKPACK\n"+Contents(_sim.State.Backpack));Button("Drink water",()=>_sim.Consume("water"));Button("Eat prepared food",()=>_sim.Consume("food"));Button("Use dressings",()=>_sim.Consume("bandage"));return;}
        bool atCamp=_player.Position.DistanceTo(_camp)<18;
        Text("MARA / COMMUNITY\nWork: "+_sim.State.WorkerJob+" · "+_sim.ProductionStatus+"\nShared storage: "+Contents(_sim.State.Camp));
        foreach(var p in _sim.Rules.Production)Button("Assign Mara: "+p.Id+" · "+Cost(p.Inputs),()=>_sim.Assign(p.Id));
        Button("Deposit supplies (camp range 18 m)",()=>{if(atCamp)_sim.Deposit();else _status="Return to camp to transfer supplies.";});
        foreach(var item in new[]{"wood","stone","fiber","plank","cloth","water","food","raw_water","raw_food","cargo"})Button("Withdraw "+item+" ×1",()=>{if(atCamp)_sim.Withdraw(item,1);else _status="Return to camp first.";});
        Button("Prepare for storm · min 6 water / 4 food",()=>{if(atCamp)_sim.PrepareStorm();});
        Text("Forecast + storm can consume seven water and food. Extra reserves or ongoing production may be needed.");
        Button("Ration food · trust -5; half food for 180 s",()=>_sim.Ration());
        Button("Supply relay · exchange 2 cargo for cloth/water",()=>{if(atCamp)_sim.Trade(_sim.NextId());});
        Button("Rest one hour · time and supplies advance",()=>{if(atCamp)_sim.Advance(75);});
        Button("Save persistent world",Save);Button("Load saved world",Load);
    }
    private string Cost(Dictionary<string,int> items)=>string.Join(", ",items.Select(p=>p.Value+" "+p.Key));
    private string Contents(Bag b)=>b.Stacks.Count==0?"Empty":string.Join(", ",b.Stacks.GroupBy(s=>s.Item).Select(g=>_sim.Rules.Items[g.Key].Name+" ×"+g.Sum(s=>s.Count)));
    private void CloseMenu(){_panel.Visible=false;_player.MenuOpen=false;_player.CaptureMouse();}
    private string SavePath(){string? forced=System.Environment.GetEnvironmentVariable("AFTERTIDE_SAVE_DIR");
        string folder=OS.HasFeature("editor")?ProjectSettings.GlobalizePath("res://outputs/aftertide-saves"):Path.Combine(OS.GetExecutablePath().GetBaseDir(),"saves");
        if(!string.IsNullOrWhiteSpace(forced)){string candidate=Path.GetFullPath(forced);string allowed=Path.GetFullPath(ProjectSettings.GlobalizePath("res://outputs/aftertide-playtests"));
            if(!candidate.StartsWith(allowed+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Test save path outside isolated AFTERTIDE area");folder=candidate;}
        Directory.CreateDirectory(folder);return Path.Combine(folder,"world-1.json");}
    private void Save(){try{_sim.State.Player=new[]{_player.Position.X,_player.Position.Y,_player.Position.Z};_sim.State.Yaw=_player.Rotation.Y;_sim.State.Pitch=_player.Pitch;
            _sim.State.Sailing=_sailing;
            if(_boat!=null){_sim.State.Boat=new[]{_boat.Position.X,_boat.Position.Y,_boat.Position.Z};_sim.State.BoatYaw=_boat.Rotation.Y;}string path=SavePath();
            File.WriteAllText(path+".tmp",_sim.Serialize());if(File.Exists(path))File.Copy(path,path+".previous",true);File.Move(path+".tmp",path,true);_status="World saved.";
        }catch(Exception ex){_status="Save failed: "+ex.Message;}}
    private void Load()=>_=LoadWorld();
    private async Task LoadWorld(){if(_loading)return;_loading=true;try{string path=SavePath();if(!File.Exists(path)){_status="No AFTERTIDE save yet.";return;}_sim.Load(File.ReadAllText(path));
            foreach(var n in _buildings)n.QueueFree();_buildings.Clear();_boat=null!;foreach(var s in _sim.State.Structures)CreateBuilding(s);
            foreach(var pair in _resourceNodes.ToArray())if(_sim.State.Depleted.Contains(pair.Key)){pair.Value.QueueFree();_resourceNodes.Remove(pair.Key);}
            if(_sim.State.Player.Length==3){var p=new Vector3(_sim.State.Player[0],_sim.State.Player[1],_sim.State.Player[2]);
                if(Mathf.Abs(p.X)<470&&Mathf.Abs(p.Z)<470&&_geo.Height(p.X,p.Z)>.2f)_player.Position=Ground(p)+Vector3.Up*.1f;else _player.Position=Ground(_camp+Vector3.Right*3)+Vector3.Up*.1f;}
            _player.Rotation=new Vector3(0,_sim.State.Yaw,0);_player.Pitch=_sim.State.Pitch;_player.Camera.Rotation=new Vector3(_sim.State.Pitch,0,0);
            if(_boat!=null&&_sim.State.Boat.Length==3){_boat.Position=new Vector3(_sim.State.Boat[0],_sim.State.Boat[1],_sim.State.Boat[2]);_boat.Rotation=new Vector3(0,_sim.State.BoatYaw,0);}
            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            _sailing=_sim.State.Sailing&&_boat!=null;_player.Sailing=_sailing;
            if(_sailing&&_boat!=null){_player.Position=_boat.Position+Vector3.Up*.8f;_lastBoat=_boat.Position;if(_boatBody!=null)_player.AddCollisionExceptionWith(_boatBody);}
            _status="Loaded persistent AFTERTIDE world.";
        }catch(Exception ex){_status="Load failed: "+ex.Message;}finally{_loading=false;}}
    public override void _ExitTree(){Input.MouseMode=Input.MouseModeEnum.Visible;}
}
