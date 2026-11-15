// Original AFTERTIDE implementation. Reference projects informed structure only.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
namespace Aftertide;

public sealed class ItemDef {public string Name{get;set;}="";public double Mass{get;set;}public double ShelfLife{get;set;}}
public sealed class Recipe {public string Id{get;set;}="";public string Name{get;set;}="";public Dictionary<string,int> Inputs{get;set;}=new();public Dictionary<string,int> Outputs{get;set;}=new();public string Requires{get;set;}="";public double Radius{get;set;}=1.5;}
public sealed class Production {public string Id{get;set;}="";public string Building{get;set;}="";public double Seconds{get;set;}public Dictionary<string,int> Inputs{get;set;}=new();public Dictionary<string,int> Outputs{get;set;}=new();}
public sealed class Quest {public string Id{get;set;}="";public string Title{get;set;}="";public string Description{get;set;}="";public string Condition{get;set;}="";public Dictionary<string,int> Reward{get;set;}=new();}
public sealed class Rules {
    public string Version{get;set;}="";public Dictionary<string,ItemDef> Items{get;set;}=new();
    public List<Recipe> Recipes{get;set;}=new();public List<Recipe> Buildings{get;set;}=new();
    public List<Production> Production{get;set;}=new();public List<Quest> Quests{get;set;}=new();
    public static readonly JsonSerializerOptions Json=new(){PropertyNameCaseInsensitive=true,WriteIndented=true};
    public static Rules Parse(string json){var rules=JsonSerializer.Deserialize<Rules>(json,Json)??throw new FormatException("Missing rules");
        foreach(var item in rules.Items.Values)if(item.Mass<=0||!double.IsFinite(item.Mass))throw new FormatException("Invalid item mass");
        foreach(var recipe in rules.Recipes.Concat(rules.Buildings))foreach(var pair in recipe.Inputs.Concat(recipe.Outputs))
            if(!rules.Items.ContainsKey(pair.Key)||pair.Value<=0)throw new FormatException("Invalid recipe");
        if(rules.Quests.Select(q=>q.Id).Distinct().Count()!=rules.Quests.Count)throw new FormatException("Duplicate quest ID");return rules;}
}
public sealed class Stack {public string Item{get;set;}="";public int Count{get;set;}public double Born{get;set;}}
public sealed class Bag {
    public double Capacity{get;set;}=25;public List<Stack> Stacks{get;set;}=new();
    public int Count(string item)=>Stacks.Where(s=>s.Item==item).Sum(s=>s.Count);
    public double Mass(Rules r)=>Stacks.Sum(s=>r.Items[s.Item].Mass*s.Count);
    public bool Add(string item,int count,double now,Rules r){if(count<=0||!r.Items.ContainsKey(item)||Mass(r)+r.Items[item].Mass*count>Capacity+1e-6)return false;Stacks.Add(new(){Item=item,Count=count,Born=now});return true;}
    public bool Take(string item,int count){if(count<=0||Count(item)<count)return false;
        foreach(var s in Stacks.Where(s=>s.Item==item).OrderBy(s=>s.Born).ToArray()){int n=Math.Min(s.Count,count);s.Count-=n;count-=n;if(count==0)break;}Stacks.RemoveAll(s=>s.Count==0);return true;}
    public bool MoveTo(Bag other,string item,int count,Rules r){if(count<=0||Count(item)<count||other.Mass(r)+r.Items[item].Mass*count>other.Capacity+1e-6)return false;
        int left=count;foreach(var s in Stacks.Where(s=>s.Item==item).OrderBy(s=>s.Born).ToArray()){int n=Math.Min(s.Count,left);other.Stacks.Add(new(){Item=item,Count=n,Born=s.Born});s.Count-=n;left-=n;if(left==0)break;}Stacks.RemoveAll(s=>s.Count==0);return true;}
}
public sealed class Structure {public string Id{get;set;}="";public string Kind{get;set;}="";public float X{get;set;}public float Y{get;set;}public float Z{get;set;}}
public sealed class SaveState {
    public int Schema{get;set;}=1;public string World{get;set;}="aftertide-jamaica-1";public int Seed{get;set;}=171511;
    public double Clock{get;set;}public Bag Backpack{get;set;}=new();public Bag Camp{get;set;}=new(){Capacity=35};
    public List<Structure> Structures{get;set;}=new();public HashSet<string> Depleted{get;set;}=new();
    public HashSet<string> Completed{get;set;}=new();public HashSet<string> Receipts{get;set;}=new();
    public double Health{get;set;}=100;public double Water{get;set;}=90;public double Food{get;set;}=90;
    public double Stamina{get;set;}=100;public double Trust{get;set;}=65;
    public bool CampChosen{get;set;}public string WorkerJob{get;set;}="";public double WorkProgress{get;set;}public int WorkCycles{get;set;}
    public bool RemoteVisited{get;set;}public bool CargoReturned{get;set;}public double BoatDistance{get;set;}
    public double StormScheduled{get;set;}=-1;public int StormsSurvived{get;set;}public bool StormSupplyFailure{get;set;}
    public int Trades{get;set;}public int LastTradeDay{get;set;}=-1;public long Sequence{get;set;}
    public float[] Player{get;set;}=Array.Empty<float>();public float[] Boat{get;set;}=Array.Empty<float>();
    public float[] CampPosition{get;set;}=Array.Empty<float>();public float Yaw{get;set;}public float Pitch{get;set;}
    public float BoatYaw{get;set;}public bool Sailing{get;set;}
    public double ConsumptionProgress{get;set;}public int Spoiled{get;set;}
    public double RationUntil{get;set;}
}
public sealed class Simulation {
    public Rules Rules{get;}public SaveState State{get;private set;}public string Notice{get;private set;}="Welcome ashore.";
    public string ProductionStatus{get;private set;}="No work assigned";public bool WorkerReady{get;set;}
    public bool Sheltered{get;set;}public bool Storm=>State.StormScheduled>=0&&State.Clock>=State.StormScheduled&&State.Clock<State.StormScheduled+90;
    public bool Rain=>Storm||((int)(State.Clock/180)%4==2);public double Hour=>(8+State.Clock/75)%24;
    public int Day=>(int)(State.Clock/1800);public Quest? CurrentQuest=>Rules.Quests.FirstOrDefault(q=>!State.Completed.Contains(q.Id));
    public Simulation(Rules rules){Rules=rules;State=new();State.Backpack.Add("wood",4,0,rules);State.Backpack.Add("fiber",3,0,rules);State.Backpack.Add("water",2,0,rules);State.Backpack.Add("food",2,0,rules);}
    public string NextId()=>"action:"+(++State.Sequence);
    public bool Has(string kind)=>State.Structures.Any(s=>s.Kind==kind);
    public bool Collect(string resource,string item,int count){if(State.Depleted.Contains(resource))return Fail("This resource has been collected.");
        if(!State.Backpack.Add(item,count,State.Clock,Rules))return Fail("Backpack full. Return supplies to camp.");State.Depleted.Add(resource);Notice=$"Collected {count} {Rules.Items[item].Name}.";Evaluate();return true;}
    public bool Exchange(Bag bag,IReadOnlyDictionary<string,int> inputs,IReadOnlyDictionary<string,int> outputs,string receipt){
        if(State.Receipts.Contains(receipt))return true;
        if(inputs.Any(p=>p.Value<=0||bag.Count(p.Key)<p.Value))return Fail("Missing materials.");
        double mass=bag.Mass(Rules)-inputs.Sum(p=>Rules.Items[p.Key].Mass*p.Value)+outputs.Sum(p=>Rules.Items[p.Key].Mass*p.Value);
        if(mass>bag.Capacity+1e-6)return Fail("Storage full; nothing consumed.");
        foreach(var p in inputs)bag.Take(p.Key,p.Value);foreach(var p in outputs)bag.Add(p.Key,p.Value,State.Clock,Rules);
        State.Receipts.Add(receipt);return true;
    }
    public bool Craft(string id,string receipt){var r=Rules.Recipes.Single(x=>x.Id==id);if(r.Requires!=""&&!Has(r.Requires))return Fail("Build "+r.Requires+" first.");
        bool ok=Exchange(State.Backpack,r.Inputs,r.Outputs,receipt);if(ok){Notice="Crafted: "+r.Name;Evaluate();}return ok;}
    public bool Build(string kind,float x,float y,float z,string receipt){if(State.Receipts.Contains(receipt))return true;
        var b=Rules.Buildings.Single(x=>x.Id==kind);if(!State.CampChosen)return Fail("Choose a camp site first.");
        if(Has(kind))return Fail("This first settlement already has that building.");
        if(!Exchange(State.Backpack,b.Inputs,new Dictionary<string,int>(),receipt))return false;
        State.Structures.Add(new(){Id="aftertide:building:"+kind,Kind=kind,X=x,Y=y,Z=z});
        if(kind=="store")State.Camp.Capacity=120;Notice="Built: "+b.Name;Evaluate();return true;}
    public void ChooseCamp(float x,float y,float z){if(State.CampChosen)return;State.CampChosen=true;State.CampPosition=new[]{x,y,z};Notice="Camp established.";Evaluate();}
    public bool Deposit(){if(!State.CampChosen)return Fail("Establish camp first.");int moved=0;
        foreach(var item in State.Backpack.Stacks.Select(s=>s.Item).Distinct().ToArray()){
            int count=State.Backpack.Count(item);if(item is "food" or "water")count=Math.Max(0,count-1);
            if(count>0&&State.Backpack.MoveTo(State.Camp,item,count,Rules)){moved+=count;if(item=="cargo"&&State.RemoteVisited)State.CargoReturned=true;}}
        Notice=moved>0?"Supplies deposited; one food and water kept in your pack.":"No transferable supplies or store full.";Evaluate();return moved>0;}
    public bool Withdraw(string item,int count){bool ok=State.Camp.MoveTo(State.Backpack,item,count,Rules);Notice=ok?"Supplies withdrawn.":"Not enough stock or backpack full.";return ok;}
    public bool Consume(string item){if(item is not ("water" or "food" or "bandage"))return Fail("Not consumable.");if(!State.Backpack.Take(item,1))return Fail("No "+item+" in your backpack.");
        if(item=="water")State.Water=Math.Min(100,State.Water+30);else if(item=="food")State.Food=Math.Min(100,State.Food+25);
        else if(item=="bandage")State.Health=Math.Min(100,State.Health+30);else throw new ArgumentException("Not consumable");
        Notice="Used: "+Rules.Items[item].Name;Evaluate();return true;}
    public void Assign(string job){if(!Rules.Production.Any(p=>p.Id==job))throw new ArgumentException("Unknown job");State.WorkerJob=job;State.WorkProgress=0;Notice="Mara assigned to "+job+". Inputs must be in shared storage.";}
    public bool PrepareStorm(){if(!Has("shelter")||State.Camp.Count("water")<6||State.Camp.Count("food")<4)return Fail("Need shelter, six stored water and four stored food.");
        if(State.StormScheduled<0||State.StormsSurvived==0&&State.Clock>State.StormScheduled+90){State.StormScheduled=State.Clock+120;State.StormSupplyFailure=false;}
        Notice="Storm forecast: two minutes. Reserves will be consumed.";Evaluate();return true;}
    public void Ration(){State.Trust=Math.Max(0,State.Trust-5);Notice="Rationing: half food use for 180 seconds; trust -5.";State.RationUntil=State.Clock+180;}
    public bool Trade(string receipt){if(!Has("trade_post"))return Fail("Build a supply relay first.");if(State.LastTradeDay==Day&&!State.Receipts.Contains(receipt))return Fail("The relay has no more cargo space today.");
        if(State.Receipts.Contains(receipt))return true;if(!Exchange(State.Camp,new Dictionary<string,int>{{"cargo",2}},new Dictionary<string,int>{{"cloth",2},{"water",3}},receipt))return false;
        State.LastTradeDay=Day;State.Trades++;Notice="Supply exchange completed: two cargo for cloth and water.";Evaluate();return true;}
    public void Advance(double seconds){if(!double.IsFinite(seconds)||seconds<0||seconds>3600)throw new ArgumentOutOfRangeException(nameof(seconds));
        while(seconds>0){double dt=Math.Min(1,seconds);seconds-=dt;bool wasStorm=Storm;State.Clock+=dt;
            State.Water=Math.Max(0,State.Water-dt*(Storm?.045:.025));State.Food=Math.Max(0,State.Food-dt*.015);
            State.Stamina=Math.Min(100,State.Stamina+dt*10);
            if(State.Water<10||State.Food<10||Storm&&!Sheltered)State.Health=Math.Max(0,State.Health-dt*(Storm&&!Sheltered?.12:.03));
            State.ConsumptionProgress+=dt;
            if(State.ConsumptionProgress>=30){State.ConsumptionProgress-=30;
                int food=State.Clock<State.RationUntil?(int)(State.Clock/30)%2:1;
                bool water=State.Camp.Take("water",1),fed=food==0||State.Camp.Take("food",food);
                if(!water||!fed){State.Trust=Math.Max(0,State.Trust-1);if(Storm)State.StormSupplyFailure=true;}
                else State.Trust=Math.Min(100,State.Trust+.2);}
            foreach(var bag in new[]{State.Backpack,State.Camp})foreach(var s in bag.Stacks.ToArray())
                if(Rules.Items[s.Item].ShelfLife>0&&State.Clock-s.Born>=Rules.Items[s.Item].ShelfLife){State.Spoiled+=s.Count;bag.Stacks.Remove(s);Notice="Food spoiled. Process fresh catch and keep reserves moving.";}
            var p=Rules.Production.FirstOrDefault(p=>p.Id==State.WorkerJob);
            ProductionStatus=p==null?"No work assigned":!Has(p.Building)?"Missing work site":!WorkerReady?"Worker travelling / route blocked":
                p.Inputs.Any(i=>State.Camp.Count(i.Key)<i.Value)?"Waiting for inputs":"Working";
            if(p!=null&&ProductionStatus=="Working"){
                State.WorkProgress+=dt;if(State.WorkProgress>=p.Seconds){
                    if(Exchange(State.Camp,p.Inputs,p.Outputs,"production:"+(++State.Sequence))){State.WorkCycles++;State.WorkProgress-=p.Seconds;}
                    else{ProductionStatus="Storage full";State.WorkProgress=p.Seconds;}}}
            if(Rain&&Has("waterworks")&&(int)State.Clock/10!=(int)(State.Clock-dt)/10)State.Camp.Add("raw_water",1,State.Clock,Rules);
            if(wasStorm&&!Storm){if(!State.StormSupplyFailure){State.StormsSurvived++;Notice="The camp endured the storm.";}else Notice="The storm exposed supply gaps. Restock and prepare again.";}
        }Evaluate();}
    public void Recover(){State.Health=55;State.Water=Math.Max(25,State.Water);State.Food=Math.Max(25,State.Food);State.Trust=Math.Max(0,State.Trust-5);
        foreach(var s in State.Backpack.Stacks)s.Count=(s.Count+1)/2;Notice="Rescued at camp: half carried supplies lost; trust -5. World preserved.";}
    private bool Condition(string id)=>id switch{
        "camp"=>State.CampChosen,"safety"=>Has("shelter")&&Has("waterworks")&&State.Camp.Count("water")+State.Backpack.Count("water")>0,
        "stores"=>Has("store")&&Has("food_rack")&&State.Camp.Count("food")>0,"work"=>State.WorkCycles>0,
        "boat"=>Has("boat"),"explore"=>State.RemoteVisited&&State.BoatDistance>=80,"return"=>State.CargoReturned,
        "prepare"=>State.StormScheduled>=0,"storm"=>State.StormsSurvived>0,"connection"=>State.Trades>0,_=>false};
    public void Evaluate(){foreach(var q in Rules.Quests){if(State.Completed.Contains(q.Id))continue;if(!Condition(q.Condition))break;
            State.Completed.Add(q.Id);foreach(var p in q.Reward)if(!State.Backpack.Add(p.Key,p.Value,State.Clock,Rules))State.Camp.Add(p.Key,p.Value,State.Clock,Rules);Notice="Objective complete: "+q.Title;}}
    public string Serialize()=>JsonSerializer.Serialize(State,Aftertide.Rules.Json);
    public void Load(string json){var next=JsonSerializer.Deserialize<SaveState>(json,Aftertide.Rules.Json)??throw new FormatException("Empty save");
        if(next.Schema!=1||next.World!="aftertide-jamaica-1"||!double.IsFinite(next.Clock)||next.Clock<0)throw new FormatException("Unsupported save");
        foreach(var bag in new[]{next.Backpack,next.Camp}){if(bag.Capacity<=0||bag.Capacity>10000)throw new FormatException("Invalid capacity");
            foreach(var stack in bag.Stacks)if(!Rules.Items.ContainsKey(stack.Item)||stack.Count<=0||stack.Count>10000||!double.IsFinite(stack.Born))throw new FormatException("Invalid stack");
            if(bag.Mass(Rules)>bag.Capacity+1e-6)throw new FormatException("Overloaded save");}
        foreach(double n in new[]{next.Health,next.Water,next.Food,next.Stamina,next.Trust})if(!double.IsFinite(n)||n<0||n>100)throw new FormatException("Invalid needs");
        if(next.Structures.Any(b=>!Rules.Buildings.Any(r=>r.Id==b.Kind)||!float.IsFinite(b.X)||!float.IsFinite(b.Y)||!float.IsFinite(b.Z))||next.Structures.Select(b=>b.Id).Distinct().Count()!=next.Structures.Count)throw new FormatException("Invalid buildings");
        if(next.Player.Any(n=>!float.IsFinite(n))||next.Boat.Any(n=>!float.IsFinite(n))||next.CampPosition.Any(n=>!float.IsFinite(n)))throw new FormatException("Invalid position");
        State=next;Notice="Persistent camp restored.";Evaluate();}
    private bool Fail(string text){Notice=text;return false;}
}
