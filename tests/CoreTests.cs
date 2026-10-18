using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Nova;
class CoreTests {
    static int failed, passed;
    static void Check(string name, Action test) { try { test(); passed++; Console.WriteLine("PASS " + name); } catch(Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e.Message); } }
    static void Eq<T>(T actual,T expected) { if(!actual.Equals(expected)) throw new Exception("Expected " + expected + ", got " + actual); }
    static void Reject(Action a) { try { a(); } catch(ArgumentException) { return; } throw new Exception("Invalid value was accepted"); }
    static int Main() {
        Check("reject NaN without writing",delegate { Reject(delegate { Rules.Validate("speed",double.NaN); }); });
        Check("reject infinite jump height",delegate { Reject(delegate { Rules.Validate("jumpHeight",double.PositiveInfinity); }); });
        Check("reject negative gravity",delegate { Reject(delegate { Rules.Validate("gravity",-1); }); });
        Check("reject fractional jump count",delegate { Reject(delegate { Rules.Validate("jumpCount",2.5); }); });
        Check("accept current upgraded grapple count",delegate { Rules.Validate("grappleCount",9001); });
        Check("reject unknown feature",delegate { Reject(delegate { Rules.Validate("notAFeature",1); }); });
        using(var m = new ProcessMemory(Process.GetCurrentProcess().Id)) {
            IntPtr block=Marshal.AllocHGlobal(128); long a=block.ToInt64();
            try {
                Marshal.WriteInt32(block,123);
                Check("read real process memory",delegate { Eq(m.Int(a),123); });
                Check("write real process memory",delegate { m.PutInt(a,321); Eq(Marshal.ReadInt32(block),321); });
                Check("invalid read fails explicitly",delegate { bool threw=false;try{m.Read(1,4);}catch{threw=true;} Eq(threw,true); });
                Check("restore first original after repeated updates",delegate { Marshal.WriteInt32(block,17); var p=new PatchLedger(m,delegate(long owner,int index,int serial){return true;});p.Set("a",a,0,0,a,BitConverter.GetBytes(99),0);Eq(Marshal.ReadInt32(block),99);p.Set("a",a,0,0,a,BitConverter.GetBytes(100),0);Eq(Marshal.ReadInt32(block),100);p.Restore("a");Eq(Marshal.ReadInt32(block),17); });
                Check("restore one bit without clobbering other flags",delegate { Marshal.WriteByte(block,0x13);var p=new PatchLedger(m,delegate(long owner,int index,int serial){return true;});p.Set("bit",a,0,0,a,new byte[]{0},0x10);Eq(Marshal.ReadByte(block),(byte)3);Marshal.WriteByte(block,0x43);p.Restore("bit");Eq(Marshal.ReadByte(block),(byte)0x53); });
                Check("do not restore a replaced object",delegate { Marshal.WriteInt32(block,7);bool valid=true;var p=new PatchLedger(m,delegate(long owner,int index,int serial){return valid;});p.Set("a",a,1,2,a,BitConverter.GetBytes(9),0);valid=false;p.Restore("a");Eq(Marshal.ReadInt32(block),9); });
                Check("uncertain identity retains originals for successful retry",delegate { Marshal.WriteInt32(block,37);bool unavailable=false;var p=new PatchLedger(m,delegate(long owner,int index,int serial){if(unavailable)throw new InvalidOperationException("temporary read failure");return true;});p.Set("a",a,1,2,a,BitConverter.GetBytes(83),0);unavailable=true;bool threw=false;try{p.RestoreAll();}catch(InvalidOperationException){threw=true;}Eq(threw,true);Eq(p.Count,1);Eq(Marshal.ReadInt32(block),83);unavailable=false;p.RestoreAll();Eq(p.Count,0);Eq(Marshal.ReadInt32(block),37); });
                Check("conflicting patches are refused",delegate { Marshal.WriteInt32(block,7);var p=new PatchLedger(m,delegate(long owner,int index,int serial){return true;});p.Set("a",a,0,0,a,BitConverter.GetBytes(9),0);bool threw=false;try{p.Set("b",a,0,0,a,BitConverter.GetBytes(10),0);}catch(InvalidOperationException){threw=true;}Eq(threw,true);p.RestoreAll();Eq(Marshal.ReadInt32(block),7); });
                Check("restore all independent patches",delegate { Marshal.WriteInt32(block,11);Marshal.WriteInt32(block,4,22);var p=new PatchLedger(m,delegate(long owner,int index,int serial){return true;});p.Set("a",a,0,0,a,BitConverter.GetBytes(1),0);p.Set("b",a,0,0,a+4,BitConverter.GetBytes(2),0);Eq(Marshal.ReadInt32(block),1);Eq(Marshal.ReadInt32(block,4),2);p.RestoreAll();Eq(Marshal.ReadInt32(block),11);Eq(Marshal.ReadInt32(block,4),22); });
            } finally { Marshal.FreeHGlobal(block); }
        }
        Console.WriteLine(passed + " passed, " + failed + " failed");return failed==0?0:1;
    }
}
