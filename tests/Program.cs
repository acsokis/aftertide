using Aftertide;
string folder=args.Length>0?args[0]:"Aftertide/Data";
var rules=Rules.Parse(File.ReadAllText(Path.Combine(folder,"gameplay.json")));
var geography=Geography.Parse(File.ReadAllText(Path.Combine(folder,"geography.json")));
Console.WriteLine("AFTERTIDE_DOMAIN_PASS checks="+Checks.Run(rules,geography));
