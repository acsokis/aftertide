using Godot;
namespace Aftertide;
public partial class Player : CharacterBody3D {
    public Camera3D Camera{get;private set;}=null!;public bool MenuOpen;public bool Sailing;
    public float Pitch;public Simulation Simulation{get;set;}=null!;
    private NavigationAgent3D _avoidance=null!;
    private double _captureWarmup=.25;
    public override void _Ready(){CollisionLayer=4;CollisionMask=11;FloorSnapLength=.4f;FloorMaxAngle=.85f;
        AddChild(new CollisionShape3D{Shape=new CapsuleShape3D{Radius=.34f,Height=1.8f},Position=Vector3.Up*.9f});
        Camera=new Camera3D{Position=Vector3.Up*1.65f,Current=true,Near=.08f,Far=2200,Fov=78};AddChild(Camera);
        _avoidance=new NavigationAgent3D{AvoidanceEnabled=true,Radius=.4f,Height=1.8f,MaxSpeed=6,AvoidancePriority=1};AddChild(_avoidance);
        foreach(var pair in new[]{("at_forward",Key.W),("at_back",Key.S),("at_left",Key.A),("at_right",Key.D),("at_jump",Key.Space),("at_sprint",Key.Shift)}){
            if(!InputMap.HasAction(pair.Item1)){InputMap.AddAction(pair.Item1);InputMap.ActionAddEvent(pair.Item1,new InputEventKey{PhysicalKeycode=pair.Item2});}}
        Input.MouseMode=Input.MouseModeEnum.Captured;}
    public override void _UnhandledInput(InputEvent e){if(e is InputEventMouseMotion m&&!MenuOpen&&_captureWarmup<=0&&System.Environment.GetEnvironmentVariable("AFTERTIDE_SMOKE")!="1"){
        RotateY(-m.Relative.X*.0022f);Pitch=Mathf.Clamp(Pitch-m.Relative.Y*.0022f,-1.3f,1.3f);Camera.Rotation=new Vector3(Pitch,0,0);}}
    public void CaptureMouse(){_captureWarmup=.15;Input.MouseMode=Input.MouseModeEnum.Captured;}
    public override void _PhysicsProcess(double delta){_captureWarmup-=delta;if(MenuOpen||Sailing){_avoidance.Velocity=Vector3.Zero;return;}float dt=(float)delta;
        var input=Input.GetVector("at_left","at_right","at_forward","at_back");var dir=GlobalBasis*new Vector3(input.X,0,input.Y);
        bool sprint=Input.IsActionPressed("at_sprint")&&Simulation.State.Stamina>5&&input.LengthSquared()>.01f;
        float speed=sprint?6:3.8f;if(sprint)Simulation.State.Stamina=Mathf.Max(0,(float)Simulation.State.Stamina-dt*18);
        var v=Velocity;v.X=Mathf.MoveToward(v.X,dir.X*speed,dt*20);v.Z=Mathf.MoveToward(v.Z,dir.Z*speed,dt*20);
        if(!IsOnFloor())v.Y-=18*dt;else if(Input.IsActionJustPressed("at_jump"))v.Y=6.5f;else v.Y=-.2f;
        Velocity=v;_avoidance.Velocity=new Vector3(v.X,0,v.Z);MoveAndSlide();}
}
