using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
namespace Nova {
    public sealed class Trainer:IDisposable {
        public readonly Unreal U;public readonly PatchLedger Patches;
        public readonly Dictionary<string,double> Values=new Dictionary<string,double>();
        public readonly HashSet<string> Toggles=new HashSet<string>();
        public long Local,Controller,Pawn,State,Movement;ObjectToken localToken,pawnToken,stateToken,moveToken;
        public bool Ready{get;private set;}public string Status{get;private set;}
        public int Health,MaxHealth,Ammo,Reserve,Grapples,MaxGrapples,Jumps;public float Mana,Adrenaline;
        public Trainer(Process p){U=new Unreal(p,true);try{U.VerifyPrivateFields();Patches=new PatchLedger(U.Mem,U.Identity);Local=U.Instance("LocalPlayer");if(Local==0)throw new InvalidOperationException("尚未建立本机玩家，请先进入游戏主菜单");localToken=U.Token(Local);Status="等待进入单人关卡";Refresh();}catch{U.Dispose();throw;}}
        public void Refresh(){
            Ready=false;
            if(U.Process.HasExited){Status="游戏已退出";return;}
            if(!U.Valid(localToken)){Status="本机玩家已失效，请重新连接";return;}
            Controller=U.Link(Local,"PlayerController");long pawn=Controller==0?0:U.Link(Controller,"Pawn");
            if(pawn==0||!U.IsA(pawn,"Human")){if(Pawn!=0){Patches.RestoreAll();Pawn=State=Movement=0;pawnToken=stateToken=moveToken=new ObjectToken();}Status="等待进入单人关卡";return;}
            if(U.Link(Controller,"Player")!=Local||U.Link(pawn,"Controller")!=Controller){Status="等待角色就绪";return;}
            long state=U.Link(pawn,"PlayerState"),movement=U.Link(pawn,"CharacterMovement");
            if(state==0||movement==0){Status="等待角色属性";return;}
            if(pawn!=Pawn||state!=State||movement!=Movement||!U.Valid(pawnToken)||!U.Valid(stateToken)||!U.Valid(moveToken)){
                var pt=U.Token(pawn);var st=U.Token(state);var mt=U.Token(movement);if(Pawn!=0)Patches.RestoreAll();Pawn=pawn;State=state;Movement=movement;pawnToken=pt;stateToken=st;moveToken=mt;
            }
            if(!U.Valid(stateToken)||!U.Valid(moveToken)){Status="角色正在切换，请稍候";return;}
            // Single player creates a listen driver too. Check real connections and player count.
            if(!U.Solo(Pawn)){Patches.RestoreAll();Status="检测到联机连接，已暂停修改";return;}
            Ready=true;Status="已连接 · 单人关卡";
        }
        void Require(){Refresh();if(!Ready)throw new InvalidOperationException(Status);}
        ObjectToken TokenFor(long o){if(o==Pawn)return pawnToken;if(o==State)return stateToken;if(o==Movement)return moveToken;return U.Token(o);}
        void Patch(string id,long obj,string field,string type,byte[] value){Field f=U.Property(obj,field,type);var t=TokenFor(obj);Patches.Set(id,t.Address,t.Index,t.Serial,obj+f.Offset,value,f.Mask);}
        void PF(string id,long obj,string field,float v){Patch(id,obj,field,"FloatProperty",BitConverter.GetBytes(v));}
        void PI(string id,long obj,string field,int v){Patch(id,obj,field,"IntProperty",BitConverter.GetBytes(v));}
        void WriteInt(long obj,string field,int value){ObjectToken t=TokenFor(obj);if(!U.Valid(t))throw new InvalidOperationException("游戏对象已变更");U.Mem.PutInt(U.Address(obj,field,"IntProperty"),value);}
        void Resource(long obj,int offset,float value){ObjectToken t=TokenFor(obj);if(!U.Valid(t))throw new InvalidOperationException("资源对象已变更");U.Mem.PutFloat(obj+offset,value);}
        public long CurrentWeapon(){long w=U.Link(Pawn,"CurrentHandTool");return w!=0&&U.IsA(w,"Weapon")&&U.Link(w,"Holder")==Pawn?w:0;}
        public long AmmoPool(long w){
            if(!U.IsA(w,"Weapon")||U.Link(w,"Holder")!=Pawn)throw new InvalidOperationException("当前武器不属于本机玩家");
            long a=U.Mem.Long(w+0x530),data=U.Mem.Long(State+0x338);int count=U.Mem.Int(State+0x340);
            if(count<1||count>64||a<data+4||a>=data+count*16L||(a-data-4)%16!=0)throw new InvalidOperationException("武器弹药池尚未就绪，请换一把枪再试");return a;
        }
        public long WeaponProxy(long original){long camera=U.Link(Controller,"PlayerCameraManager");if(camera==0)return 0;Field f;if(!U.Fields(U.Mem.Long(camera+0x10)).TryGetValue("ProxyHandTool",out f)||f.Type!="ObjectProperty")return 0;long proxy=U.Link(camera,"ProxyHandTool");if(proxy==0||!U.IsA(proxy,"Weapon")||U.Link(proxy,"MyOriginal")!=original||U.Link(proxy,"Holder")!=camera)return 0;return proxy;}
        void SetMagazine(long w,int value){var t=U.Token(w);if(!U.Valid(t))throw new InvalidOperationException("武器对象已变更");Field f;if(U.Fields(U.Mem.Long(w+0x10)).TryGetValue("Ammunition Current",out f)){if(f.Type!="IntProperty")throw new InvalidOperationException("弹匣蓝图属性不兼容");U.Mem.PutInt(w+f.Offset,value);}U.Mem.PutInt(w+0x528,value);}
        void FillWeapon(long w,int value){var t=U.Token(w);long pool=AmmoPool(w);long proxy=WeaponProxy(w);if(!U.Valid(t)||!U.Valid(stateToken))throw new InvalidOperationException("武器已切换");U.Mem.PutInt(pool,value);SetMagazine(w,value);if(proxy!=0)SetMagazine(proxy,value);}
        public void Apply(string id,double value){
            Rules.Validate(id,value);Require();
            if(id=="adrenaline"){Resource(State,0x388,(float)value);return;}
            if(id=="ammo"){long w=CurrentWeapon();if(w==0)throw new InvalidOperationException("请先装备一把枪械");FillWeapon(w,(int)value);return;}
            Values[id]=value;Tick();if(!Ready)throw new InvalidOperationException(Status);if(id=="grappleCount")WriteInt(Pawn,"GrappleShots",Math.Min(U.Int(Pawn,"GrappleShots"),(int)value));
        }
        public void Action(string id){Require();switch(id){
            case "heal":WriteInt(Pawn,"MyShield",Math.Max(1,U.Int(Pawn,"MaxShield")));WriteInt(Pawn,"MyHP",Math.Max(3,U.Int(Pawn,"MyHP")));break;
            case "lowHealth":if(Toggles.Contains("health"))throw new InvalidOperationException("请先关闭无限生命");WriteInt(Pawn,"MyShield",1);break;
            case "refillMana":Resource(State,0x420,100);break;
            case "lowMana":if(Toggles.Contains("mana"))throw new InvalidOperationException("请先关闭无限法力");Resource(State,0x420,5);break;
            case "refillGrapple":WriteInt(Pawn,"GrappleShots",Math.Max(1,U.Int(Pawn,"MaxGrappleShots")));break;
            default:throw new ArgumentException("未知操作");}
            ReadStats();
        }
        public void Toggle(string id,bool enabled){
            if(!new[]{"health","mana","adrenalineLock","ammoLock","grappleLock","jumpLock","grappleRange"}.Contains(id))throw new ArgumentException("未知开关");
            if(enabled)Require();
            if(enabled)Toggles.Add(id);else{Toggles.Remove(id);Patches.Restore(id);if(id=="jumpLock"&&!Values.ContainsKey("jumpCount"))Patches.Restore("jumpCount");}
            if(Ready)Tick();
        }
        public void ResetValue(string id){Values.Remove(id);if(id!="jumpCount"||!Toggles.Contains("jumpLock"))Patches.Restore(id);if(Ready)Tick();}
        public void ResetAll(){Values.Clear();Toggles.Clear();Patches.RestoreAll();}
        public void Tick(){
            Refresh();if(!Ready)return;
            if(Toggles.Contains("health")){Patch("health",Pawn,"bCanBeDamaged","BoolProperty",new byte[]{0});WriteInt(Pawn,"MyShield",Math.Max(1,U.Int(Pawn,"MaxShield")));WriteInt(Pawn,"MyHP",Math.Max(3,U.Int(Pawn,"MyHP")));}
            if(Toggles.Contains("mana"))Resource(State,0x420,100);
            if(Toggles.Contains("adrenalineLock"))Resource(State,0x388,100);
            if(Toggles.Contains("grappleLock"))WriteInt(Pawn,"GrappleShots",Math.Max(1,U.Int(Pawn,"MaxGrappleShots")));
            if(Toggles.Contains("ammoLock")){long w=CurrentWeapon();if(w!=0)FillWeapon(w,9999);}
            double v;
            if(Values.TryGetValue("manaRate",out v))PF("manaRate",Pawn,"ManaRechargeRate",(float)v);
            if(Values.TryGetValue("speed",out v))PF("speed",Pawn,"CustomTimeDilation",(float)v);
            if(Values.TryGetValue("jumpHeight",out v))PF("jumpHeight",Movement,"JumpZVelocity",(float)v);
            if(Values.TryGetValue("gravity",out v)){PF("gravity",Movement,"GravityScale",(float)v);PF("gravity",Pawn,"CachedDefaultGravity",(float)v);}
            if(Values.TryGetValue("grappleCount",out v)){PI("grappleCount",Pawn,"MaxGrappleShots",(int)v);PI("grappleCount",Pawn,"BaseGrappleShots",(int)v);}
            if(Toggles.Contains("jumpLock"))PI("jumpCount",Pawn,"JumpMaxCount",1000000);else if(Values.TryGetValue("jumpCount",out v))PI("jumpCount",Pawn,"JumpMaxCount",(int)v);
            if(Toggles.Contains("grappleRange")){PF("grappleRange",Pawn,"GrapMinRange",0);PF("grappleRange",Pawn,"GrapMaxRange",10000000);PF("grappleRange",Pawn,"GrappleHookLength",10000000);PF("grappleRange",Pawn,"HaloGrappleHookLength",10000000);}
        }
        public void ReadStats(){if(!Ready)return;Health=U.Int(Pawn,"MyShield");MaxHealth=U.Int(Pawn,"MaxShield");Mana=U.Mem.Float(State+0x420);Adrenaline=U.Mem.Float(State+0x388);Grapples=U.Int(Pawn,"GrappleShots");MaxGrapples=U.Int(Pawn,"MaxGrappleShots");Jumps=U.Int(Pawn,"JumpMaxCount");long w=CurrentWeapon();Ammo=w==0?-1:U.Mem.Int(w+0x528);try{Reserve=w==0?-1:U.Mem.Int(AmmoPool(w));}catch{Reserve=-1;}}
        public void Dispose(){ResetAll();U.Dispose();}
    }
}
