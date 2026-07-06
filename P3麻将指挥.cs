using Dalamud.Utility.Numerics;
using KodakkuAssist.Data;
using KodakkuAssist.Extensions;
using KodakkuAssist.Module.Draw;
using KodakkuAssist.Module.GameEvent;
using KodakkuAssist.Module.GameEvent.Struct;
using KodakkuAssist.Module.GameOperate;
using KodakkuAssist.Script;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Threading;

namespace FF14脚本
{
    [ScriptType(name: "绝凯夫卡p3麻将指挥", territorys: [1363], guid: "9116F56A-2123-5A29-6466-040E8FA0A060", version: "1.0.0.18", author: "XQY")]
    public class P3麻将指挥模式
    {
        #region 用户设置

        [UserSetting("指挥模式")]
        public static bool 指挥模式 { get; set; } = false;

        [UserSetting("P3一运给火buff两人上禁止12")]
        public static bool P3一运标点 { get; set; } = false;

        [UserSetting("默语调试")]
        public static bool 调试 { get; set; } = false;

        [UserSetting("是否本地标点")]
        public static bool 本地标点 { get; set; } = false;

        [UserSetting("发送到小队")]
        public static bool 发送到小队 { get; set; } = false;

        [UserSetting("发送可视化麻将顺序到小队")]
        public static bool 发送可视化麻将顺序到小队 { get; set; } = false;

        #endregion
        #region 各种变量
        List<Vector3> 究极冲击波次数 = new List<Vector3>();
        Vector3 场中 = new Vector3(100, 0, 100);
        int 冲锋顺逆 = 0;//1顺 -1逆 0默认
        int 麻将顺逆 = 0;//1顺 -1逆 0默认
        int 麻将起点 = -1;
        bool 麻将数据收集完成 = false;
        int 麻将编号 = 0;
        int 收集次数 = 0;
        
        Dictionary<int, int> _麻将编号 = new Dictionary<int, int>
        {
            {336, 1},
            {337, 2 },
            {338, 3},
            {339, 4},
            {437, 5},
            {438, 6},
            {439, 7},
            {440, 8}
        };
        Dictionary<int, string> 队伍优先级字典 = new Dictionary<int, string>
        {
            {0 ,"MT" },
            {1 ,"ST" },
            {2 ,"H1" },
            {3 ,"H2" },
            {4 ,"D1" },
            {5 ,"D2" },
            {6 ,"D3" },
            {7 ,"D4" }

        };
        List<string> 麻将安全区 = new List<string> { "A2之间", "2B之间", "B3之间", "3C之间", "C4之间", "4D之间", "D1之间", "1A之间", "不知道" };
        List<int> 所有人的麻将 = Enumerable.Repeat(-1, 8).ToList();
        List<int> 麻将编号2 = new List<int> { 1,2, 3, 4, 5, 6, 7, 8 };
        MarkType[] 标点 = new MarkType[] { MarkType.Attack1, MarkType.Attack2, MarkType.Attack3, MarkType.Attack4, MarkType.Attack5, MarkType.Attack6, MarkType.Attack7, MarkType.Attack8 };
        IReadOnlyList<MarkType> 火点名标点 = new List<MarkType> { MarkType.Stop1, MarkType.Stop2 };
        List<int> 火buff = new List<int>();
        Dictionary<int,string> 八方字典 = new Dictionary<int, string>
        {
            {0 ,"A" },
            {1 ,"2" },
            {2 ,"B" },
            {3 ,"3" },
            {4 ,"C" },
            {5 ,"4" },
            {6 ,"D" },
            {7 ,"1" }
        };
        Dictionary<int, string> 顺逆字典 = new Dictionary<int, string>
        {
            {1 ,"顺时针" },
            {-1 ,"逆时针" },
            {0 ,"未知"  }
            
        };

        #endregion



        #region 数据收集
        /// <summary>
        /// 收集究极冲击波起点和顺逆用来判断麻将起点和顺逆。
        /// </summary>
        [ScriptMethod(eventType: EventTypeEnum.ActionEffect, name: "收集究极冲击波", eventCondition: ["ActionId:47843"], userControl: false, suppress: 500)]
        public void 收集究极冲击波(Event e, ScriptAccessory ac)
        {
            if (麻将数据收集完成) return;
            string SourcePosition = e["SourcePosition"];

            if (究极冲击波次数.Count < 2) { 究极冲击波次数.Add(辅助方法_.ParseToVector3Newtonsoft(SourcePosition)); }
            else if (究极冲击波次数.Count == 2) 
            {
                麻将数据收集完成 = true; 
                if(调试)
                {
                    辅助方法_.发送默语(ac, "究极冲击波数据收集完成");
                }
               
            }
            else { 辅助方法_.发送默语(ac, $"究极冲击波数据收集中,当前收集次数: {究极冲击波次数.Count}"); }

            if (究极冲击波次数.Count == 2 && !麻将数据收集完成)
            {
                int dir1 = 辅助方法_.GetClockDirection(场中, 究极冲击波次数[0], 8);
                int dir2 = 辅助方法_.GetClockDirection(场中, 究极冲击波次数[1], 8);
                冲锋顺逆 = 辅助方法_.判断顺逆(dir1, dir2);
                (麻将起点, 麻将顺逆) = 辅助方法_.计算麻将起点(dir1, 冲锋顺逆);


                string 究极冲击波顺逆 = 冲锋顺逆 switch
                {
                    1 => "顺时针",
                    -1 => "逆时针",
                    _ => "未知"
                };
                if(调试)
                {
                    辅助方法_.发送默语(ac, $"究极冲击波顺逆: {究极冲击波顺逆}, 麻将起点: {麻将起点}, 麻将顺逆: {(麻将顺逆 == 1 ? "顺时针" : 麻将顺逆 == -1 ? "逆时针" : "未知")}");
                }
            }

        }
        /// <summary>
        /// 收集所有人的麻将编号,用于标记和指挥。
        /// </summary>
        [ScriptMethod(eventType: EventTypeEnum.VfxEvent, name: "收集所有人麻将", eventCondition: ["Id:regex:^(33[6-9]|43[7-9]|440)$"], userControl: false)]
        public void 收集所有人麻将(Event e, ScriptAccessory ac)
        {
            string id = e["TargetId"];
            uint _id = 辅助方法_.HexToUint(id);
            int 某人的麻将编号 = _麻将编号[Convert.ToInt32(e["Id"])];
            int indx = 辅助方法_.GetPlayerIdIndex(ac, _id);
            所有人的麻将[某人的麻将编号 - 1] = indx;
            收集次数++;
            if (调试)
            {
                辅助方法_.发送默语(ac, $"收集成功,次数:{收集次数}");
            }
        }
        /// <summary>
        /// 收集活buff的两人,用于标记和指挥。
        /// </summary>

        [ScriptMethod(eventType: EventTypeEnum.StatusAdd, name: "收集火buff的两人", eventCondition: ["SourceId:E0000000"], userControl: false)]
        public void 收集火buff的两人(Event e, ScriptAccessory ac)
        {
            string id = e["TargetId"];
            string buffidstr = e["StatusID"];
            if (uint.TryParse(buffidstr, out uint buffid)) { if (buffid != 1600) return; }
            uint _id = 辅助方法_.HexToUint(id);
            int indx = 辅助方法_.GetPlayerIdIndex(ac, _id);
            火buff.Add(indx);
        }
        #endregion

        #region 执行
        /// <summary>
        ///团灭或战斗重置。
        /// </summary>

        public void Init(ScriptAccessory ac)
        {
            究极冲击波次数.Clear();
            冲锋顺逆 = 0;
            麻将顺逆 = 0;
            麻将起点 = -1;
            麻将编号 = 0;
            麻将数据收集完成 = false;
            收集次数 = 0;
            所有人的麻将 = Enumerable.Repeat(-1, 8).ToList();
            火buff.Clear();
            究极冲击波次数.Clear();
            ac.Method.MarkClear();

            if(调试)辅助方法_.发送默语(ac, "数据已重置");
        }

        [ScriptMethod(eventType: EventTypeEnum.VfxEvent, name: "麻将给所有人标点", eventCondition: ["Id:regex:^(33[6-9]|43[7-9]|440)$"], userControl: false, suppress: 500)]
        public async void 麻将给所有人标点(Event e, ScriptAccessory ac)
        {
            await Task.Delay(500);
            if(!指挥模式) return;
            if (收集次数 < 8) { 辅助方法_.发送默语(ac, "收集数据不完全"); return; }
            if(所有人的麻将.Contains(-1)) { 辅助方法_.发送默语(ac, "收集数据不完全"); return; }

            var 正确排序的数组 = 辅助方法_.根据顺逆右移数组(麻将起点, 麻将顺逆, 所有人的麻将);

            List<string> 消息列表 = new List<string>(); 
            for (int j = 0; j < 正确排序的数组.Count; j++)
            {
                队伍优先级字典.TryGetValue(正确排序的数组[j], out string str2);
                消息列表.Add($"{str2}去{麻将安全区[j]}");
            }
            for (int i = 0; i < 正确排序的数组.Count; i++)
            {
                辅助方法_.队伍索引标点(ac, 正确排序的数组[i], 标点[i], 本地标点);
                队伍优先级字典.TryGetValue(正确排序的数组[i], out string str);
                

                if (调试)
                {
                    辅助方法_.发送默语(ac, $"第{i + 1}次,给{正确排序的数组[i]},{str}标上了点");
                    
                }
                
               
            }
            

            await Task.Delay(10000);
            if(收集次数 == 8) 
            {
                ac.Method.MarkClear();
                辅助方法_.发送默语(ac, "已清除标点");
            }
            
        }
        [ScriptMethod(eventType: EventTypeEnum.ActionEffect, name: "龙卷风发送麻将消息", eventCondition: ["ActionId:47864"], userControl: false,suppress: 500)]
        public void 龙卷风发送麻将消息(Event e, ScriptAccessory ac)
        {
            if (发送可视化麻将顺序到小队) 
            {
                var 正确的麻将排序 = 辅助方法_.根据顺逆右移数组(麻将起点, 麻将顺逆, 麻将编号2);
                var 可视化消息列表 = new List<string>();
                可视化消息列表.Add($"---------------------"); 
                可视化消息列表.Add($"        {正确的麻将排序[7]}    {正确的麻将排序[0]}        ");
                可视化消息列表.Add($"    {正确的麻将排序[6]}            {正确的麻将排序[1]}    ");
                可视化消息列表.Add($"    {正确的麻将排序[5]}            {正确的麻将排序[2]}    ");
                可视化消息列表.Add($"        {正确的麻将排序[4]}    {正确的麻将排序[3]}        ");
                可视化消息列表.Add($"---------------------");
                辅助方法_.按顺序发送小队(ac, 可视化消息列表, 收集次数);
                if(调试) 默语调试辅助方法.发送默语调试(ac, 可视化消息列表, 收集次数);
            }
            if(收集次数 != 8)  return;
            List<string> 消息列表 = new List<string>();
            var 正确排序的数组2 = 辅助方法_.根据顺逆右移数组(麻将起点, 麻将顺逆, 所有人的麻将);
            八方字典.TryGetValue(麻将起点, out string 麻将起点str);
            顺逆字典.TryGetValue(麻将顺逆, out string 麻将顺逆str);
            消息列表.Add($"麻将起点:{麻将起点str}");
            消息列表.Add($"麻将顺逆:{麻将顺逆str}");

            if (调试) 
            {
                if (麻将顺逆 == 1)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        队伍优先级字典.TryGetValue(正确排序的数组2[j], out string str2);
                        消息列表.Add($"{str2}去{麻将安全区[j]}");
                    }
                }
                else if (麻将顺逆 == -1)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        队伍优先级字典.TryGetValue(正确排序的数组2[j], out string str2);
                        消息列表.Add($"{str2}去{麻将安全区[j]}");
                    }
                }
                else { 消息列表.Add("麻将顺逆未知,无法发送小队消息"); }
                默语调试辅助方法.发送默语调试(ac, 消息列表, 收集次数);
            }
            if (发送到小队)
            {
                if (麻将顺逆 == 1)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        队伍优先级字典.TryGetValue(正确排序的数组2[j], out string str2);
                        消息列表.Add($"{str2}去{麻将安全区[j]}");
                    }
                }
                else if (麻将顺逆 == -1)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        队伍优先级字典.TryGetValue(正确排序的数组2[j], out string str2);
                        消息列表.Add($"{str2}去{麻将安全区[j]}");
                    }
                }
                else { 消息列表.Add("麻将顺逆未知,无法发送小队消息"); }
                ;
                辅助方法_.按顺序发送小队(ac, 消息列表, 收集次数);
            }

        }

        /// <summary>
        /// 给火buff的两人标点,用于指挥。
        /// </summary>

        [ScriptMethod(eventType: EventTypeEnum.StatusAdd, name: "给火buff的两人标点", eventCondition: ["StatusID:1600"], userControl: false, suppress: 500)]
        public async void 给火buff两人标点(Event e, ScriptAccessory ac)
        {
            await Task.Delay(500);
            string str = e["StatusID"];
            if (str != "1600") return;
            if(!P3一运标点) return;

            if (火buff.Count < 2 ) { if(调试)辅助方法_.发送默语(ac, "收集火buff的两人不完全"); return; }
            else if(火buff.Count == 2)
            {
                for(int i = 0; i < 火buff.Count; i++)
                {
                    辅助方法_.队伍索引标点(ac, 火buff[i], 火点名标点[i], 本地标点);
                    
                }
                if (调试)
                {
                    队伍优先级字典.TryGetValue(火buff[0], out string str1);
                    队伍优先级字典.TryGetValue(火buff[1], out string str2);
                    辅助方法_.发送默语(ac, $"给{str1}标上了火点名\n 给{str2}标上了火点名");
                }
            }
        }
        [ScriptMethod(eventType: EventTypeEnum.StartCasting, name: "真空波清除火buff标点", eventCondition: ["ActionId:47891"], userControl: false)]
        public void 真空波清除火buff标点(Event e, ScriptAccessory ac)
        {
            if(!P3一运标点)return;
            if (火buff.Count > 0)
            {
                ac.Method.MarkClear();
                
                if (调试) 辅助方法_.发送默语(ac, "已清除火buff标点");
            }
        }
    }
        #endregion
 }
    public static class 辅助方法_
    {
        /// <summary>
        /// 输入玩家的 PID 返回(int)其在队伍列表中的索引。
        /// </summary>

        public static int GetPlayerIdIndex(this ScriptAccessory sa, uint pid)
        {

            return sa.Data.PartyList.IndexOf(pid);
        }
        /// <summary>
        /// 将十六进制字符串（可带 "0x" 前缀）转换为 <see cref="uint"/> 数值。
        /// 转换失败或输入为空时返回 0。
        /// </summary>
        public static uint HexToUint(string hexStr)
        {
            if (string.IsNullOrEmpty(hexStr)) return 0;

            hexStr = hexStr.Replace("0x", "").Replace("0X", "");

            if (uint.TryParse(hexStr, System.Globalization.NumberStyles.HexNumber, null, out uint result))
                return result;

            return 0;
        }
        /// <summary>
        /// 使用 Newtonsoft.Json 将 JSON 字符串转换为 Vector3（大写字段）。
        /// </summary>
        public static Vector3 ParseToVector3Newtonsoft(string json)
        {
            if (string.IsNullOrEmpty(json)) return Vector3.Zero;
            return JsonConvert.DeserializeObject<Vector3>(json);
        }
        /// <summary>
        /// 对队伍中指定索引的玩家施加一个标记（或其他 <see cref="MarkType"/>）。
        /// </summary>

        public static void MarkPlayerByIdx(this ScriptAccessory sa, int idx, MarkType marker, bool local = false)
        {
            sa.Method.Mark(sa.Data.PartyList[idx], marker, local);
        }
        public static Vector3 ParseToVector3(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            float x = root.GetProperty("X").GetSingle();
            float y = root.GetProperty("Y").GetSingle();
            float z = root.GetProperty("Z").GetSingle();

            return new Vector3(x, y, z);
        }


        /// <summary>
        /// 计算目标点相对于中心点的时钟方位编号。
        /// 坐标系：X+ = 东，Z+ = 南。
        /// 编号规则（顺时针）：0 = 正北，依次向东、南、西递增。
        /// </summary>
        /// <param name="center">场地中心点（或参考基准点）</param>
        /// <param name="target">目标点坐标</param>
        /// <param name="divisions">等分数（如 4 表示四方位，8 表示八方位）</param>
        /// <returns>方位编号 0 ~ (divisions - 1)</returns>
        public static int GetClockDirection(Vector3 center, Vector3 target, int divisions)
        {
            if (divisions <= 0)
                throw new ArgumentException("等分数必须大于 0");

            // 1. 计算相对偏移量 (忽略高度 Y，只看平面 XZ)
            float dx = target.X - center.X; // 东为正
            float dz = target.Z - center.Z; // 南为正

            // 2. 防止除零（目标点与中心点重合）
            if (Math.Abs(dx) < 0.0001f && Math.Abs(dz) < 0.0001f)
                return 0;

            // 3. 计算从正北开始，顺时针方向的角度 [0, 2π)
            // 在 (X=东, Z=南) 坐标系中：
            // Atan2(dx, -dz) 完美契合：
            //   北(0, -1) -> Atan2(0, 1) = 0
            //   东(1,  0) -> Atan2(1, 0) = π/2
            //   南(0,  1) -> Atan2(0,-1) = π
            //   西(-1, 0) -> Atan2(-1,0)= -π/2
            double angle = Math.Atan2(dx, -dz);
            if (angle < 0) angle += 2 * Math.PI; // 将 [-π, π] 转换到 [0, 2π]

            // 4. 将角度映射到等分编号
            double step = 2 * Math.PI / divisions;

            // 加上半个步长，让正北(0度)落在编号 0 的区间中心，而不是边界上
            double shiftedAngle = angle + (step / 2.0);

            int directionIndex = (int)(shiftedAngle / step) % divisions;

            return directionIndex;
        }
        /// <summary>
        /// 判断从方向 <paramref name="dir1"/> 到 <paramref name="dir2"/> 的旋转方向。
        /// </summary>
        public static int 判断顺逆(int dir1, int dir2)
        {
            int diff = (dir2 - dir1 + 8) % 8;
            if (diff == 0) return 0;
            if (diff < 4) return 1;   // 顺时针
            if (diff > 4) return -1;  // 逆时针
            return 0;
        }
        public static (int, int) 计算麻将起点(int dir1, int 冲锋顺逆)
        {
            int dir2 = (dir1 + 4) % 8;
            int 麻将顺逆 = 0;
            if (冲锋顺逆 == 1) { 麻将顺逆 = -1; }
            else if (冲锋顺逆 == -1) { 麻将顺逆 = 1; }
            else { 麻将顺逆 = 0; }
            return (dir2, 麻将顺逆);
        }
        public static bool 是否自己(Event e, ScriptAccessory ac, string TargetId)
        {
            var myid = ac.Data.Me;


            // 将十六进制字符串转为 uint
            uint targetId = uint.TryParse(
                TargetId.Replace("0x", ""),
                System.Globalization.NumberStyles.HexNumber,
                null,
                out var result) ? result : 0;

            return targetId == myid;
        }
        public static List<int> 麻将顺时针右移数组(int 麻将起点, List<int> list)
        {
        if (list.Contains(-1) || 麻将起点 < 0 || 麻将起点 > 8) return list;
        if (麻将起点 == 0) return list;
        int k = 麻将起点; 
        int n = list.Count;
        List<int> result = list.Skip(n - k).Concat(list.Take(n - k)).ToList();
        return result;
          }
        public static List<int> 麻将逆时针左移数组(int 麻将起点, List<int> list)
        {
          if (list.Contains(-1) || 麻将起点 < 0 || 麻将起点 > 8) return list    ;
          List<int> newlist = new List<int>();
          newlist = list.Reverse<int>().ToList();
        if(麻将起点 == 0) return newlist;
        int k = 麻将起点;
        int n = list.Count;
        List<int> result = newlist.Skip(n-k).Concat(newlist.Take(n-k)).ToList();
        return result;

    }
        public static void 发送默语(ScriptAccessory ac, string 默语内容)
        {
            if(string.IsNullOrWhiteSpace(默语内容)) return;
            ac.Method.SendChat($"/e {默语内容}");
        }
        public static List<int> 根据顺逆右移数组(int 麻将起点, int 麻将顺逆, List<int> list)
        {
            if (麻将顺逆 == 0) return list;
            else if (麻将顺逆 == 1) return 麻将顺时针右移数组(麻将起点, list);
            else return 麻将逆时针左移数组(麻将起点, list);
        }
        public static void 队伍索引标点(this ScriptAccessory sa, int idx, MarkType marker, bool local = false)
        {
           if(idx < 0 || idx >8) { 辅助方法_.发送默语(sa, $"队伍索引{idx}不在范围内,无法标点"); return; }
        
    
           sa.Method.Mark(sa.Data.PartyList[idx], marker, local);
        }
        public static void 按顺序发送小队(ScriptAccessory ac, List<string> 默语内容,int 版本号)
        {
            if (默语内容 == null || 默语内容.Count == 0) return;
            Task.Run(async () => 
            {
                if(版本号 == 0) { return; }

                for(int i = 0; i < 默语内容.Count; i++)
                {
                    ac.Method.SendChat($"/p {默语内容[i]}");
                    if (i + 1 < 默语内容.Count)
                    {
                        await Task.Delay(150);
                    }
                }

            });
            
        }
        public static void 按顺序发送默语(ScriptAccessory ac, List<string> 默语内容, int 版本号)
        {
            if (默语内容 == null || 默语内容.Count == 0) return;
            Task.Run(async () =>
            {
                if (版本号 == 0) { return; }

                for (int i = 0; i < 默语内容.Count; i++)
                {
                    ac.Method.SendChat($"/e {默语内容[i]}");
                    if (i + 1 < 默语内容.Count)
                    {
                        await Task.Delay(150);
                    }
                }

            });

        }


    }
public static class  默语调试辅助方法
{
    private static readonly SemaphoreSlim _mutex = new SemaphoreSlim(1, 1);
    public static void 发送默语调试(ScriptAccessory ac, List<string> 默语内容, int 版本号)
    {
        
        if (默语内容 == null || 默语内容.Count == 0) return;
        if (版本号 == 0) return; 


        Task.Run(async () =>
        {
           
            await _mutex.WaitAsync();
            try
            {
               
                for (int i = 0; i < 默语内容.Count; i++)
                {
                    ac.Method.SendChat($"/e {默语内容[i]}");
                    if (i + 1 < 默语内容.Count)
                    {
                        await Task.Delay(150);
                    }
                }
            }
            finally
            {
               
                _mutex.Release();
            }
        });
    }
}


