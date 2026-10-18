using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
namespace Nova {
    public sealed class ProcessMemory:IDisposable {
        [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
        [DllImport("kernel32.dll",SetLastError=true)] static extern bool ReadProcessMemory(IntPtr process,IntPtr address,byte[] buffer,UIntPtr length,out UIntPtr done);
        [DllImport("kernel32.dll",SetLastError=true)] static extern bool WriteProcessMemory(IntPtr process,IntPtr address,byte[] buffer,UIntPtr length,out UIntPtr done);
        [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
        IntPtr handle;
        public ProcessMemory(int pid) { handle=OpenProcess(0x438,false,pid); if(handle==IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(),"无法打开游戏进程"); }
        public static bool Pointer(long address) {return address>=0x10000 && address<0x7fffffffffff;}
        public bool TryRead(long address,int count,out byte[] buffer) {
            buffer=null;if(!Pointer(address)||count<1||count>16*1024*1024||!Pointer(address+count-1)||handle==IntPtr.Zero)return false;
            var b=new byte[count];UIntPtr done;if(!ReadProcessMemory(handle,new IntPtr(address),b,(UIntPtr)count,out done)||done.ToUInt64()!=(ulong)count)return false;buffer=b;return true;
        }
        public byte[] Read(long address,int count) { byte[] b;if(!TryRead(address,count,out b))throw new InvalidOperationException("游戏对象已失效或内存不可读");return b; }
        public int Int(long a){return BitConverter.ToInt32(Read(a,4),0);}
        public uint UInt(long a){return BitConverter.ToUInt32(Read(a,4),0);}
        public long Long(long a){return BitConverter.ToInt64(Read(a,8),0);}
        public float Float(long a){return BitConverter.ToSingle(Read(a,4),0);}
        public byte Byte(long a){return Read(a,1)[0];}
        public void Write(long address,byte[] b){UIntPtr done;if(b==null||b.Length<1||!Pointer(address)||!Pointer(address+b.Length-1))throw new ArgumentException("无效写入地址");if(!WriteProcessMemory(handle,new IntPtr(address),b,(UIntPtr)b.Length,out done)||done.ToUInt64()!=(ulong)b.Length)throw new Win32Exception(Marshal.GetLastWin32Error(),"游戏内存写入失败");}
        public void PutInt(long a,int v){Write(a,BitConverter.GetBytes(v));}
        public void PutFloat(long a,float v){if(float.IsNaN(v)||float.IsInfinity(v))throw new ArgumentException("数值必须有限");Write(a,BitConverter.GetBytes(v));}
        public void Dispose(){if(handle!=IntPtr.Zero){CloseHandle(handle);handle=IntPtr.Zero;}}
    }
    public sealed class PatchLedger {
        sealed class Patch { public string Feature;public long Owner,Address;public int Index,Serial;public byte[] Original;public byte Mask; }
        readonly ProcessMemory mem;readonly Func<long,int,int,bool> valid;readonly List<Patch> patches=new List<Patch>();
        public int Count{get{return patches.Count;}}
        public PatchLedger(ProcessMemory mem,Func<long,int,int,bool> valid){this.mem=mem;this.valid=valid;}
        public void Set(string feature,long owner,int index,int serial,long address,byte[] value,byte mask){
            if(value==null||value.Length==0||mask!=0&&value.Length!=1)throw new ArgumentException("无效修改数据");
            if(!valid(owner,index,serial))throw new InvalidOperationException("角色对象发生变化，已停止修改");
            Patch p=patches.Find(x=>x.Feature==feature&&x.Address==address);
            if(p!=null&&(p.Owner!=owner||p.Index!=index||p.Serial!=serial)){patches.Remove(p);p=null;}
            if(p==null){
                if(patches.Any(x=>x.Address<address+value.Length&&address<x.Address+x.Original.Length&&(x.Mask==0||mask==0||(mask&x.Mask)!=0)))throw new InvalidOperationException("多个修改项目发生地址冲突");
                p=new Patch{Feature=feature,Owner=owner,Index=index,Serial=serial,Address=address,Original=mem.Read(address,value.Length),Mask=mask};patches.Add(p);
            }
            if(p.Mask!=mask||p.Original.Length!=value.Length)throw new InvalidOperationException("修改数据格式发生变化");
            if(mask!=0)value=new byte[]{(byte)((mem.Byte(address)&~mask)|(value[0]&mask))};
            mem.Write(address,value);
        }
        public void Restore(string feature){
            var errors=new List<string>();foreach(var p in patches.Where(x=>feature==null||x.Feature==feature).ToArray()){
                try{if(valid(p.Owner,p.Index,p.Serial)){byte[] v=p.Original;if(p.Mask!=0)v=new byte[]{(byte)((mem.Byte(p.Address)&~p.Mask)|(p.Original[0]&p.Mask))};mem.Write(p.Address,v);}patches.Remove(p);}catch(Exception e){errors.Add(e.Message);}
            }
            if(errors.Count>0)throw new InvalidOperationException("部分原值恢复失败："+errors[0]);
        }
        public void RestoreAll(){Restore(null);}
    }
    public static class Rules {
        public static void Validate(string id,double value){
            double min=0,max=0;bool integer=false;
            switch(id){
                case "adrenaline":max=100;break;
                case "manaRate":max=1000;break;
                case "ammo":max=99999;integer=true;break;
                case "grappleCount":min=1;max=100000;integer=true;break;
                case "jumpCount":min=1;max=1000;integer=true;break;
                case "speed":min=0.1;max=10;break;
                case "jumpHeight":min=100;max=20000;break;
                case "gravity":min=0;max=20;break;
                default:throw new ArgumentException("未知修改项目");
            }
            if(double.IsNaN(value)||double.IsInfinity(value)||value<min||value>max||integer&&value!=Math.Truncate(value))throw new ArgumentException("数值超出允许范围 "+min+"～"+max);
        }
    }
}
