// 컴파일 참조 전용 파사드: 설치된(구버전) Assembly-CSharp.dll에 r149+ 전용 API를 덧붙인다.
// 출력은 빌드 참조로만 쓰이고 게임 폴더에는 들어가지 않는다.
using dnlib.DotNet;

var mod = ModuleDefMD.Load(args[0]);

// 신버전(r149+) HitMargin 값: 출시 DLL의 IL 상수에서 역산 (XPerfect=4, Midspin=14, switch(hit-2) 구조)
var values = new Dictionary<string, int> {
    ["TooEarly"] = 0, ["VeryEarly"] = 1, ["EarlyPerfect"] = 2, ["PerfectMinus"] = 3, ["XPerfect"] = 4,
    ["PerfectPlus"] = 5, ["LatePerfect"] = 6, ["VeryLate"] = 7, ["TooLate"] = 8, ["Multipress"] = 9,
    ["FailMiss"] = 10, ["FailOverload"] = 11, ["Auto"] = 12, ["OverPress"] = 13, ["Midspin"] = 14,
};
var hm = mod.Types.Single(t => t.Name == "HitMargin" && t.Namespace == "");
foreach (var f in hm.Fields.Where(f => f.IsStatic && !values.ContainsKey(f.Name)).ToList()) hm.Fields.Remove(f);
foreach (var (name, value) in values) {
    var f = hm.Fields.FirstOrDefault(x => x.Name == name);
    if (f == null) {
        f = new FieldDefUser(name, new FieldSig(new ValueTypeSig(hm)),
            FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault);
        hm.Fields.Add(f);
    }
    f.Constant = new ConstantUser(value, ElementType.I4);
}

// scrMarginTracker.xScore (int 필드)
var smt = mod.Types.Single(t => t.Name == "scrMarginTracker" && t.Namespace == "");
if (!smt.Fields.Any(f => f.Name == "xScore"))
    smt.Fields.Add(new FieldDefUser("xScore", new FieldSig(mod.CorLibTypes.Int32), FieldAttributes.Public));

// static class HitMarginHelper { public static int ToXScore(this HitMargin m); }  (본문은 컴파일 참조용이라 의미 없음)
if (!mod.Types.Any(t => t.Name == "HitMarginHelper")) {
    var helper = new TypeDefUser("", "HitMarginHelper", mod.CorLibTypes.Object.TypeDefOrRef) {
        Attributes = TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
    };
    var m = new MethodDefUser("ToXScore",
        MethodSig.CreateStatic(mod.CorLibTypes.Int32, new ValueTypeSig(hm)),
        MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig) { Body = new dnlib.DotNet.Emit.CilBody() };
    m.Body.Instructions.Add(dnlib.DotNet.Emit.OpCodes.Ldc_I4_0.ToInstruction());
    m.Body.Instructions.Add(dnlib.DotNet.Emit.OpCodes.Ret.ToInstruction());
    // 확장 메서드 표시
    var extAttr = mod.CorLibTypes.GetTypeRef("System.Runtime.CompilerServices", "ExtensionAttribute");
    helper.Methods.Add(m);
    mod.Types.Add(helper);
}
mod.Write(args[1]);
Console.WriteLine("facade written: " + args[1]);
