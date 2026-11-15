using System;
using System.Collections.Generic;
using System.Linq;
namespace Aftertide;
public static class Checks {
    public static int Run(Rules rules,Geography geo){int checks=0;
        void Check(bool ok,string message){checks++;if(!ok)throw new InvalidOperationException("AFTERTIDE_CHECK_FAIL "+message);}
        var sim=new Simulation(rules);int serial=0;
        sim.ChooseCamp(0,2,0);Check(sim.State.Completed.Contains("camp"),"Camp goal");
        void Gather(string item,int count){while(count>0){int n=Math.Min(3,count);if(!sim.Collect("test:resource:"+(serial++),item,n)){
                    sim.Deposit();Check(sim.Collect("test:resource:"+(serial++),item,n),"Gather capacity recovery");}count-=n;}}
        void Ensure(string item,int count){int missing=count-sim.State.Backpack.Count(item);if(missing<=0)return;
            int fromStore=Math.Min(missing,sim.State.Camp.Count(item));if(fromStore>0){Check(sim.Withdraw(item,fromStore),"Withdraw");missing-=fromStore;}
            if(missing<=0)return;
            if(item is "plank" or "cloth"){
                var recipe=rules.Recipes.Single(r=>r.Outputs.ContainsKey(item));while(sim.State.Backpack.Count(item)<count){foreach(var p in recipe.Inputs)Ensure(p.Key,p.Value);Check(sim.Craft(recipe.Id,sim.NextId()),"Craft "+recipe.Id);}}
            else Gather(item,missing);
        }
        void Build(string kind){var b=rules.Buildings.Single(b=>b.Id==kind);foreach(var p in b.Inputs)Ensure(p.Key,p.Value);
            string id=sim.NextId();Check(sim.Build(kind,serial++,2,3,id),"Build "+kind);int n=sim.State.Structures.Count;
            Check(sim.Build(kind,0,0,0,id)&&sim.State.Structures.Count==n,"Idempotent construction");}
        Build("shelter");Build("waterworks");Check(sim.State.Completed.Contains("safety"),"Safety goal");
        Build("store");Build("food_rack");sim.Deposit();Check(sim.State.Completed.Contains("stores"),"Store goal");
        Gather("raw_water",12);Gather("wood",6);sim.Deposit();sim.Assign("water");sim.WorkerReady=true;sim.Advance(9);
        Check(sim.State.WorkCycles>0&&sim.State.Completed.Contains("work"),"Real production consumes inputs and completes work objective");
        Build("boat");Check(sim.State.Completed.Contains("boat"),"Boat objective");
        sim.State.RemoteVisited=true;sim.State.BoatDistance=100;sim.Evaluate();Check(sim.State.Completed.Contains("explore"),"Expedition objective");
        Gather("cargo",8);sim.Deposit();Check(sim.State.Completed.Contains("return"),"Cargo transport objective");
        Gather("wood",18);Gather("raw_food",10);sim.Deposit();sim.Assign("food");sim.Advance(33);
        Gather("raw_water",15);sim.Deposit();sim.Assign("water");sim.Advance(33);
        Check(sim.State.Camp.Count("water")>=6&&sim.State.Camp.Count("food")>=4,"Forecast reserves produced");
        Check(sim.PrepareStorm(),"Forecast begins from real reserves");sim.Sheltered=true;sim.Advance(211);
        Check(sim.State.StormsSurvived>0&&sim.State.Completed.Contains("storm"),"Supplied storm survived");
        Build("trade_post");string trade=sim.NextId();Check(sim.Trade(trade),"Finite supply exchange");
        int trades=sim.State.Trades,cargo=sim.State.Camp.Count("cargo");Check(sim.Trade(trade)&&sim.State.Trades==trades&&sim.State.Camp.Count("cargo")==cargo,"Replay cannot duplicate trade");
        Check(!sim.Trade(sim.NextId()),"Daily finite exchange limit");Check(sim.State.Completed.Count==10,"Complete ten-objective loop");
        sim.Ration();string save=sim.Serialize();var loaded=new Simulation(rules);loaded.Load(save);
        Check(loaded.State.Completed.Count==10&&loaded.State.RationUntil==sim.State.RationUntil,"Saved objectives and ration policy");
        Check(loaded.State.Camp.Count("cargo")==sim.State.Camp.Count("cargo")&&loaded.State.Receipts.SetEquals(sim.State.Receipts),"Stock and receipt roundtrip");
        int completed=loaded.State.Completed.Count;loaded.Evaluate();Check(loaded.State.Completed.Count==completed,"No repeated quest reward on load");
        var before=loaded.State;try{loaded.Load(save.Replace("aftertide-jamaica-1","invalid-world"));Check(false,"Bad world accepted");}catch(FormatException){}
        Check(ReferenceEquals(before,loaded.State),"Invalid save preserves current state");
        var small=new Bag{Capacity=.1};Check(!small.Add("wood",1,0,rules),"Carry capacity");
        var source=new Bag();source.Add("raw_food",2,10,rules);var target=new Bag();Check(source.MoveTo(target,"raw_food",1,rules)&&target.Stacks[0].Born==10,"Transfer preserves food age");
        var spoil=new Simulation(rules);spoil.State.Backpack.Add("raw_food",2,0,rules);spoil.Advance(481);Check(spoil.State.Backpack.Count("raw_food")==0&&spoil.State.Spoiled>=2,"Food spoilage");
        var p=geo.Geographic(125,-80);var inverse=geo.Local(p.Longitude,p.Latitude);Check(Math.Abs(inverse.X-125)<1e-6&&Math.Abs(inverse.Z+80)<1e-6,"Geographic roundtrip");
        Check(Geography.Distance(-77,18,-77,19)>111000&&Geography.Distance(-77,18,-77,19)<111300,"One degree latitude metric check");
        Check(Geography.Distance(-77,18,-76,18)<Geography.Distance(-77,18,-77,19),"Longitude is not latitude distance");
        foreach(string id in new[]{"CUB","JAM","HTI","DOM","PRI","CYM","BHS"})Check(geo.Countries.Any(c=>c.Iso==id),"Geographic country "+id);
        return checks;
    }
}
