using MeterForge.Domain.Common;
using MeterForge.Domain.Rating;
using MeterForge.Application.Usage;
using MeterForge.Infrastructure.Usage;

var checks=0;
void Check(bool condition,string name) { checks++; if(!condition) throw new Exception(name); }
void Reject(Action action,string code) {
    try { action(); throw new Exception($"Expected {code}"); }
    catch(BusinessRuleException e) { Check(e.Code==code,$"Expected {code}, got {e.Code}"); }
}
Check(new Money(1.20m," usd ").Currency=="USD","currency normalization");
Check(new Money(-1m,"USD").Amount==-1m,"negative representation characterization");
Check(new Money(1m,"123").Currency=="123","length-only currency characterization");
Reject(()=>new Money(0.005m,"USD"),"money.precision_invalid");
Reject(()=>new Money(1m,"US"),"money.currency_invalid");
Reject(()=>{_ = new Money(1m,"USD")+new Money(1m,"EUR");},"money.currency_mismatch");
UsageTier[] tiers=[new(10m,new Money(.10m,"USD")),new(null,new Money(.05m,"USD"))];
for(var quantity=0;quantity<=200;quantity++) {
    var rated=TieredRater.Rate(quantity,tiers);
    var expected=quantity<=10?quantity*.10m:1m+(quantity-10)*.05m;
    Check(rated.Total.Amount==expected,$"integer oracle quantity {quantity}");
    Check(rated.Tiers.Sum(x=>x.Units)==quantity,$"coverage quantity {quantity}");
    Check(TieredRater.Rate(quantity,tiers.Reverse().ToArray()).Total==rated.Total,$"tier permutation {quantity}");
}
Check(TieredRater.Rate(.05m,[new(null,new Money(.10m,"USD"))]).Total.Amount==0m,"even midpoint down");
Check(TieredRater.Rate(.15m,[new(null,new Money(.10m,"USD"))]).Total.Amount==.02m,"even midpoint up");
var one=TieredRater.Rate(.10m,[new(null,new Money(.10m,"USD"))]);
var split=TieredRater.Rate(.10m,[new(.05m,new Money(.10m,"USD")),new(null,new Money(.10m,"USD"))]);
Check(one.Total.Amount==.01m && split.Total.Amount==0m,"rounding partition counterexample");
var incomplete=TieredRater.Rate(12m,[new(10m,new Money(.10m,"USD"))]);
Check(incomplete.Tiers.Sum(x=>x.Units)==10m && incomplete.Quantity==12m,"finite coverage characterization");
var store=new InMemoryUsageEventStore(); var service=new UsageIngestionService(store);
var subscription=Guid.Parse("00000000-0000-0000-0000-000000000001");
var instant=DateTimeOffset.Parse("2026-01-01T12:00:00Z");
var first=await service.IngestAsync("T",subscription," requests ",2m,instant," E ",default);
var replay=await service.IngestAsync("T",subscription,"requests",2.00m,instant.ToOffset(TimeSpan.FromHours(2)),"E",default);
Check(!first.IsReplay && replay.IsReplay && first.Event.Id==replay.Event.Id,"normalized replay");
try { await service.IngestAsync("T",subscription,"requests",3m,instant,"E",default); throw new Exception("Expected conflict"); }
catch(BusinessRuleException e) { Check(e.Code=="usage.idempotency_conflict","changed quantity conflict"); }
Check((await store.FindByIdAsync("T",first.Event.Id,default))!.Quantity==2m,"conflict preserves winner");
Check(await store.FindByIdAsync("other",first.Event.Id,default) is null,"tenant-scoped lookup");
var padded=await service.IngestAsync(" T ",subscription,"requests",2m,instant,"E",default);
Check(!padded.IsReplay && padded.Event.Id!=first.Event.Id,"tenant not trimmed characterization");
var wave=await Task.WhenAll(Enumerable.Range(0,64).Select(_=>Task.Run(()=>service.IngestAsync("T",subscription,"requests",4m,instant,"wave",default))));
Check(wave.Select(x=>x.Event.Id).Distinct().Count()==1,"wave one stored identity");
Check(wave.Count(x=>!x.IsReplay)==1,"wave one original winner");
using var cancelled=new CancellationTokenSource(); cancelled.Cancel();
try { await service.IngestAsync("T",subscription,"requests",1m,instant,"cancelled",cancelled.Token); throw new Exception("Expected cancellation"); }
catch(OperationCanceledException) { Check(true,"pre-cancelled request rejected"); }
Console.WriteLine($"{checks} actual-source studio assertions passed. In-memory and pure arithmetic only; no durable database or provider execution.");
