using System;
using System.Diagnostics;
using System.Linq;
using Nova;
class UnrealTests {
    static int Main(){try{var p=Process.GetProcessesByName("Greylock-Win64-Shipping").Single();using(var u=new Unreal(p,true)){
        if(u.Name(0)!="None")throw new Exception("Name pool");u.VerifyPrivateFields();long local=u.Instance("LocalPlayer");long pc=u.Link(local,"PlayerController");long pawn=u.Link(pc,"Pawn");long state=u.Link(pawn,"PlayerState");long move=u.Link(pawn,"CharacterMovement");
        Console.WriteLine("Local player -> "+u.ClassName(pc)+" -> "+u.ClassName(pawn));if(u.ClassName(pawn)!="BP_Human_Player_C")throw new Exception("Wrong pawn");var token=u.Token(pawn);if(!u.Valid(token))throw new Exception("Object token");if(u.Valid(pawn,token.Index,token.Serial+1))throw new Exception("Stale object accepted");
        Console.WriteLine("HP "+u.Int(pawn,"MyHP")+"; shield "+u.Int(pawn,"MyShield")+" / "+u.Int(pawn,"MaxShield"));Console.WriteLine("Mana "+u.Mem.Float(state+0x420)+"; adrenaline "+u.Mem.Float(state+0x388));Console.WriteLine("Jump "+u.Int(pawn,"JumpMaxCount")+"; gravity "+u.Float(move,"GravityScale"));
        long world=u.World(pawn);if(world==0||!u.Solo(pawn))throw new Exception("Single-player validation");Console.WriteLine("PASS verified engine properties, private getter signatures, local player ownership, serial protection and solo world");}
        return 0;}catch(Exception e){Console.WriteLine("FAIL "+e);return 1;}}
}
