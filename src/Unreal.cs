using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
namespace Nova {
    public struct ObjectToken { public long Address;public int Index,Serial; }
    public sealed class Field { public string Name,Type;public int Offset,Size;public byte Mask;public long Meta; }
    public sealed class Unreal:IDisposable {
        public const string SupportedHash="0a310bb464f67656f48e2d428937275d539b581a1136a4cf09bd6cea215a5e91";
        public readonly Process Process; public readonly ProcessMemory Mem;public readonly long Base,Pool,Objects;
        readonly Dictionary<uint,string> names=new Dictionary<uint,string>();
        readonly Dictionary<long,Dictionary<string,Field>> fields=new Dictionary<long,Dictionary<string,Field>>();
        List<long> objectList;
        public Unreal(Process process,bool verify){
            Process=process;
            if(verify){using(var h=SHA256.Create())using(var s=File.OpenRead(process.MainModule.FileName)){string hash=BitConverter.ToString(h.ComputeHash(s)).Replace("-","").ToLowerInvariant();if(hash!=SupportedHash)throw new InvalidOperationException("游戏版本未适配。此版支持 3.23 / Steam 22752386，请勿强制写入。");}}
            Mem=new ProcessMemory(process.Id);Base=process.MainModule.BaseAddress.ToInt64();Pool=Base+0x573acc0;Objects=Base+0x5777010;
            try {if(Name(0)!="None")throw new InvalidOperationException("游戏名称池校验失败");ReloadObjects();}
            catch{Mem.Dispose();throw;}
        }
        public string Name(uint index){
            string n;if(names.TryGetValue(index,out n))return n;
            if((index>>16)>8191)throw new InvalidOperationException("游戏名称索引无效");
            long block=Mem.Long(Pool+0x10+8*(index>>16));long a=block+2*(index&65535);ushort h=BitConverter.ToUInt16(Mem.Read(a,2),0);int length=h>>6;
            if(length<1||length>1023)throw new InvalidOperationException("名称池格式不兼容");
            n=((h&1)!=0?Encoding.Unicode:Encoding.UTF8).GetString(Mem.Read(a+2,length*((h&1)!=0?2:1)));names[index]=n;return n;
        }
        public string ObjectName(long address){return address==0?"":Name(Mem.UInt(address+0x18));}
        public string ClassName(long address){return address==0?"":ObjectName(Mem.Long(address+0x10));}
        public void ReloadObjects(){
            int count=Mem.Int(Objects+20);if(count<1||count>2000000)throw new InvalidOperationException("游戏对象表尚未就绪");
            long chunks=Mem.Long(Objects);var list=new List<long>(count);
            for(int i=0;i<(count+65535)/65536;i++){byte[] b=Mem.Read(Mem.Long(chunks+8*i),Math.Min(65536,count-65536*i)*24);for(int j=0;j<b.Length;j+=24){long a=BitConverter.ToInt64(b,j);uint flags=BitConverter.ToUInt32(b,j+8);if(a!=0&&(flags&0x30000000)==0)list.Add(a);}}
            objectList=list;
        }
        public long Class(string name){foreach(long o in objectList){try{if(ObjectName(o)==name){string cn=ClassName(o);if(cn=="Class"||cn=="BlueprintGeneratedClass")return o;}}catch{}}throw new InvalidOperationException("未找到游戏类型："+name);}
        public long Instance(string className){long cls=Class(className);foreach(long o in objectList){try{if(Mem.Long(o+0x10)==cls&&(Mem.UInt(o+8)&0x10)==0)return o;}catch{}}return 0;}
        public Dictionary<string,Field> Fields(long cls){
            Dictionary<string,Field> result;if(fields.TryGetValue(cls,out result))return result;
            result=new Dictionary<string,Field>();long root=cls;var classes=new HashSet<long>();
            while(cls!=0&&classes.Count<32&&classes.Add(cls)){
                long p=Mem.Long(cls+0x50);var seen=new HashSet<long>();
                while(p!=0&&seen.Count<2048&&seen.Add(p)){
                    var f=new Field{Name=Name(Mem.UInt(p+0x28)),Type=Name(Mem.UInt(Mem.Long(p+8))),Offset=Mem.Int(p+0x4c),Size=Mem.Int(p+0x3c),Meta=p};
                    if(f.Offset<0||f.Offset>0x100000||f.Size<1||f.Size>0x100000)throw new InvalidOperationException("属性结构校验失败");
                    if(f.Type=="BoolProperty"){byte[] b=Mem.Read(p+0x78,4);f.Offset+=b[1];f.Mask=b[3];if(f.Mask==0)throw new InvalidOperationException("布尔属性格式不兼容");}
                    if(!result.ContainsKey(f.Name))result[f.Name]=f;p=Mem.Long(p+0x20);
                }
                cls=Mem.Long(cls+0x40);
            }
            fields[root]=result;return result;
        }
        public Field Property(long obj,string name,string type){
            Field f;if(!Fields(Mem.Long(obj+0x10)).TryGetValue(name,out f)||type!=null&&f.Type!=type)throw new InvalidOperationException("属性不兼容："+name);return f;
        }
        public long Address(long obj,string name,string type){return obj+Property(obj,name,type).Offset;}
        public long Link(long obj,string name){return Mem.Long(Address(obj,name,"ObjectProperty"));}
        public int Int(long obj,string name){return Mem.Int(Address(obj,name,"IntProperty"));}
        public float Float(long obj,string name){return Mem.Float(Address(obj,name,"FloatProperty"));}
        public ObjectToken Token(long obj){int index=Mem.Int(obj+0xc);long item=Item(index);if(Mem.Long(item)!=obj)throw new InvalidOperationException("游戏对象索引不匹配");return new ObjectToken{Address=obj,Index=index,Serial=Mem.Int(item+0x10)};}
        long Item(int index){if(index<0||index>=Mem.Int(Objects+20))throw new InvalidOperationException("游戏对象已释放");long chunk=Mem.Long(Mem.Long(Objects)+8*(index/65536));return chunk+(index%65536)*24L;}
        public bool Identity(long obj,int index,int serial){if(Process.HasExited)return false;int count=Mem.Int(Objects+20);if(count<1||count>2000000)throw new InvalidOperationException("对象表暂时不可用，请稍后重试还原");if(index<0||index>=count)return false;long item=Item(index);return Mem.Long(item)==obj&&Mem.Int(item+0x10)==serial&&(Mem.UInt(item+8)&0x30000000)==0&&Mem.Int(obj+0xc)==index;}
        public bool Valid(long obj,int index,int serial){try{return Identity(obj,index,serial);}catch{return false;}}
        public bool Valid(ObjectToken t){return t.Address!=0&&Valid(t.Address,t.Index,t.Serial);}
        public long World(long obj){for(int i=0;i<8&&obj!=0;i++){if(ClassName(obj)=="World")return obj;obj=Mem.Long(obj+0x20);}return 0;}
        public bool IsA(long obj,string expected){long cls=Mem.Long(obj+0x10);for(int i=0;i<32&&cls!=0;i++){if(ObjectName(cls)==expected)return true;cls=Mem.Long(cls+0x40);}return false;}
        public bool Solo(long pawn){long w=World(pawn);if(w==0)return false;long state=Link(w,"GameState");if(state==0)return false;long array=Address(state,"PlayerArray","ArrayProperty");if(Mem.Int(array+8)!=1)return false;long net=Link(w,"NetDriver");if(net==0)return true;return Link(net,"ServerConnection")==0&&Mem.Int(Address(net,"ClientConnections","ArrayProperty")+8)==0;}
        public void VerifyPrivateFields(){
            // These getters belong to the exact executable fingerprint checked above.
            if(!Mem.Read(Base+0x18e2c25,6).SequenceEqual(new byte[]{0x8b,0x81,0x20,0x04,0,0})||!Mem.Read(Base+0x1921ec5,6).SequenceEqual(new byte[]{0x8b,0x81,0x28,0x05,0,0})||!Mem.Read(Base+0x1921d83,7).SequenceEqual(new byte[]{0x48,0x8b,0x81,0x30,0x05,0,0})||!Mem.Read(Base+0x17792ac,8).SequenceEqual(new byte[]{0xf3,0x0f,0x10,0x83,0x88,0x03,0,0}))throw new InvalidOperationException("私有属性的原生读取函数校验失败");
        }
        public void Dispose(){Mem.Dispose();}
    }
}
