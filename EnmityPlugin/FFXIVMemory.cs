using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

// 2017/06/20 1:12:38: Info: EnmityDebug: DEBUG: MyCharacter: 'Amazon Prime' (268717602) 224E0410

namespace Tamagawa.EnmityPlugin
{
    public class FFXIVMemory : IDisposable
    {
        public class MemoryScanException : Exception
        {
            public MemoryScanException() : base(Messages.FailedToSigScan) { }
            public MemoryScanException(string message) : base(message) { }
            public MemoryScanException(string message, System.Exception inner) : base(message, inner) { }
            protected MemoryScanException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        }

        private Thread _thread;
        internal List<Combatant> Combatants { get; private set; }
        internal object CombatantsLock => new object();

        private const string charmapSignature = "488b420848c1e8033da701000077248bc0488d0d";
        private const string targetSignature = "41bc000000e041bd01000000493bc47555488d0d";
        private const string enmitySignature = "83f9ff7412448b048e8bd3488d0d";
        private const int charmapOffset = 0;
        private const int targetOffset = 192;
        private const int enmityOffset = -4648;

        private const int combatantDataSize = 16192; //0x3F40

        private EnmityOverlay _overlay;
        private Process _process;
        private FFXIVClientMode _mode;

        private IntPtr charmapAddress = IntPtr.Zero;
        private IntPtr targetAddress = IntPtr.Zero;
        private IntPtr enmityAddress = IntPtr.Zero;
        private IntPtr aggroAddress = IntPtr.Zero;

        public FFXIVMemory(EnmityOverlay overlay, Process process)
        {
            _overlay = overlay;
            _process = process;
            if (process.ProcessName == "ffxiv")
            {
                _mode = FFXIVClientMode.FFXIV_32;
                throw new MemoryScanException(String.Format("DX9 is not supported."));
            }
            else if (process.ProcessName == "ffxiv_dx11")
            {
                _mode = FFXIVClientMode.FFXIV_64;
            }
            else
            {
                _mode = FFXIVClientMode.Unknown;
            }
            overlay.LogDebug("Attatching process: {0} ({1})",
                process.Id, (_mode == FFXIVClientMode.FFXIV_64 ? "dx11" : "other"));

            this.GetPointerAddress();

            Combatants = new List<Combatant>();

            _thread = new Thread(new ThreadStart(DoScanCombatants))
            {
                IsBackground = true
            };
            _thread.Start();

            overlay.LogInfo("Attatched process successfully, pid: {0} ({1})",
                process.Id, (_mode == FFXIVClientMode.FFXIV_64 ? "dx11" : "other"));
        }

        public void Dispose()
        {
            _overlay.LogDebug("FFXIVMemory Instance disposed");
            _thread.Abort();
        }

        private void DoScanCombatants()
        {
            List<Combatant> c;
            while (true)
            {
                Thread.Sleep(125);

                if (!this.ValidateProcess())
                {
                    Thread.Sleep(1000);
                    return;
                }

                c = this._getCombatantList();
                lock (CombatantsLock)
                {
                    this.Combatants = c;
                }
            }
        }

        public enum FFXIVClientMode
        {
            Unknown = 0,
            FFXIV_32 = 1,
            FFXIV_64 = 2,
        }

        public Process Process
        {
            get
            {
                return _process;
            }
        }

        public bool ValidateProcess()
        {
            if (_process == null)
            {
                return false;
            }
            if (_process.HasExited)
            {
                return false;
            }
            if (charmapAddress == IntPtr.Zero ||
                enmityAddress == IntPtr.Zero ||
                targetAddress == IntPtr.Zero)
            {
                return GetPointerAddress();
            }
            return true;
        }

        /// <summary>
        /// 各ポインタのアドレスを取得
        /// </summary>
        private bool GetPointerAddress()
        {
            bool success = true;
            bool bRIP = true;

            List<string> fail = new List<string>();

            /// CHARMAP
            List<IntPtr> list = SigScan(charmapSignature, 0, bRIP);
            if (list != null && list.Count == 1)
            {
                charmapAddress = list[0] + charmapOffset;
            }
            else
            {
                charmapAddress = IntPtr.Zero;
                fail.Add(nameof(charmapAddress));
                success = false;
            }

            // ENMITY
            list = SigScan(enmitySignature, 0, bRIP);
            if (list != null && list.Count == 1)
            {
                enmityAddress = list[0] + enmityOffset;
                aggroAddress = IntPtr.Add(enmityAddress, 2312); // Offset 0x900 + 8
            }
            else
            {
                enmityAddress = IntPtr.Zero;
                aggroAddress = IntPtr.Zero;
                fail.Add(nameof(enmityAddress));
                fail.Add(nameof(aggroAddress));
                success = false;
            }

            /// TARGET
            list = SigScan(targetSignature, 0, bRIP);
            if (list != null && list.Count == 1)
            {
                targetAddress = list[0] + targetOffset;
            }
            else
            {
                targetAddress = IntPtr.Zero;
                fail.Add(nameof(targetAddress));
                success = false;
            }

            _overlay.LogDebug("charmapAddress: 0x{0:X}", charmapAddress.ToInt64());
            _overlay.LogDebug("enmityAddress: 0x{0:X}", enmityAddress.ToInt64());
            _overlay.LogDebug("targetAddress: 0x{0:X}", targetAddress.ToInt64());
            Combatant c = GetSelfCombatant();
            if (c != null)
            {
                _overlay.LogDebug("MyCharacter: '{0}' ({1})", c.Name, c.ID);
            }

            if (!success)
            {
                throw new MemoryScanException(String.Format(Messages.FailedToSigScan, String.Join(",", fail)));
            }

            return success;
        }

        /// <summary>
        /// カレントターゲットの情報を取得
        /// </summary>
        public Combatant GetTargetCombatant()
        {
            Combatant target = null;
            IntPtr address = IntPtr.Zero;

            byte[] source = GetByteArray(targetAddress, 128);
            unsafe
            {
                fixed (byte* p = source) address = new IntPtr(*(Int64*)p);
            }
            if (address.ToInt64() <= 0)
            {
                return null;
            }

            source = GetByteArray(address, combatantDataSize);
            target = GetCombatantFromByteArray(source);
            return target;
        }

        /// <summary>
        /// 自キャラの情報を取得
        /// </summary>
        public Combatant GetSelfCombatant()
        {
            Combatant self = null;
            IntPtr address = (IntPtr)GetUInt64(charmapAddress);
            if (address.ToInt64() > 0)
            {
                byte[] source = GetByteArray(address, combatantDataSize);
                self = GetCombatantFromByteArray(source);
            }
            return self;
        }

        /// <summary>
        /// アンカーターゲット情報を取得
        /// </summary>
        public Combatant GetAnchorCombatant()
        {
            Combatant target = null;
            IntPtr address = IntPtr.Zero;

            int offset = 0x08;
            byte[] source = GetByteArray(IntPtr.Add(targetAddress, offset), 128);
            unsafe
            {
                fixed (byte* p = source) address = new IntPtr(*(Int64*)p);
            }
            if (address.ToInt64() <= 0)
            {
                return null;
            }

            source = GetByteArray(address, combatantDataSize);
            target = GetCombatantFromByteArray(source);
            return target;
        }

        /// <summary>
        /// フォーカスターゲット情報を取得
        /// </summary>
        public Combatant GetFocusCombatant()
        {
            Combatant target = null;
            IntPtr address = IntPtr.Zero;

            int offset = 0x58;
            byte[] source = GetByteArray(IntPtr.Add(targetAddress, offset), 128);
            unsafe
            {
                fixed (byte* p = source) address = new IntPtr(*(Int64*)p);
            }
            if (address.ToInt64() <= 0)
            {
                return null;
            }

            source = GetByteArray(address, combatantDataSize);
            target = GetCombatantFromByteArray(source);
            return target;
        }

        /// <summary>
        /// ホバーターゲット情報を取得
        /// </summary>
        public Combatant GetHoverCombatant()
        {
            Combatant target = null;
            IntPtr address = IntPtr.Zero;

            int offset = 0x20;
            byte[] source = GetByteArray(IntPtr.Add(targetAddress, offset), 128);
            unsafe
            {
                fixed (byte* p = source) address = new IntPtr(*(Int64*)p);
            }
            if (address.ToInt64() <= 0)
            {
                return null;
            }

            source = GetByteArray(address, combatantDataSize);
            target = GetCombatantFromByteArray(source);
            return target;
        }

        /// <summary>
        /// 周辺のキャラ情報を取得
        /// </summary>
        private unsafe List<Combatant> _getCombatantList()
        {
            int num = 344;
            List<Combatant> result = new List<Combatant>();

            int sz = 8;
            byte[] source = GetByteArray(charmapAddress, sz * num);
            if (source == null || source.Length == 0) { return result; }

            for (int i = 0; i < num; i++)
            {
                IntPtr p;
                fixed (byte* bp = source) p = new IntPtr(*(Int64*)&bp[i * sz]);

                if (!(p == IntPtr.Zero))
                {
                    byte[] c = GetByteArray(p, combatantDataSize);
                    Combatant combatant = GetCombatantFromByteArray(c);
                    if (combatant.type != ObjectType.PC && combatant.type != ObjectType.Monster)
                    {
                        continue;
                    }
                    if (combatant.ID != 0 && combatant.ID != 3758096384u && !result.Exists((Combatant x) => x.ID == combatant.ID))
                    {
                        combatant.Order = i;
                        result.Add(combatant);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// メモリのバイト配列からキャラ情報に変換
        /// </summary>
        public unsafe Combatant GetCombatantFromByteArray(byte[] source)
        {
            int offset = 0;
            Combatant combatant = new Combatant();
            fixed (byte* p = source)
            {
                // For Debug
                //combatant.BoA = BitConverter.ToString(source);

                combatant.Name = GetStringFromBytes(source, 48);
                combatant.ID = *(uint*)&p[116];
                combatant.OwnerID = *(uint*)&p[132];
                if (combatant.OwnerID == 3758096384u)
                {
                    combatant.OwnerID = 0u;
                }
                combatant.type = (ObjectType)p[140];
                combatant.EffectiveDistance = p[146];

                offset = 160;
                combatant.PosX = *(Single*)&p[offset];
                combatant.PosZ = *(Single*)&p[offset + 4];
                combatant.PosY = *(Single*)&p[offset + 8];
                combatant.Heading = *(Single*)&p[offset + 16];

                combatant.TargetID = *(uint*)&p[5832];

                offset = 5960;
                if (combatant.type == ObjectType.PC || combatant.type == ObjectType.Monster)
                {
                    combatant.CurrentHP = *(int*)&p[offset + 8];
                    combatant.MaxHP = *(int*)&p[offset + 12];
                    combatant.CurrentMP = *(int*)&p[offset + 16];
                    combatant.MaxMP = *(int*)&p[offset + 20];
                    combatant.CurrentTP = *(short*)&p[offset + 24];
                    combatant.MaxTP = 1000;
                    combatant.Job = p[offset + 64];
                    combatant.Level = p[offset + 66];

                    // Status aka Buff,Debuff
                    combatant.Statuses = new List<Status>();
                    const int StatusEffectOffset = 6152;
                    const int statusSize = 12;

                    int statusCountLimit = 60;
                    if (combatant.type == ObjectType.PC) statusCountLimit = 30;

                    var statusesSource = new byte[statusCountLimit * statusSize];
                    Buffer.BlockCopy(source, StatusEffectOffset, statusesSource, 0, statusCountLimit * statusSize);
                    for (var i = 0; i < statusCountLimit; i++)
                    {
                        var statusBytes = new byte[statusSize];
                        Buffer.BlockCopy(statusesSource, i * statusSize, statusBytes, 0, statusSize);
                        var status = new Status
                        {
                            StatusID = BitConverter.ToInt16(statusBytes, 0),
                            Stacks = statusBytes[2],
                            Duration = BitConverter.ToSingle(statusBytes, 4),
                            CasterID = BitConverter.ToUInt32(statusBytes, 8),
                            IsOwner = false,
                        };

                        if (status.IsValid())
                        {
                            combatant.Statuses.Add(status);
                        }
                    }

                    // Cast
                    combatant.Casting = new Cast
                    {
                        ID = *(short*)&p[6900],
                        TargetID = *(uint*)&p[6912],
                        Progress = *(Single*)&p[6948],
                        Time = *(Single*)&p[6952],
                    };
                }
                else
                {
                    combatant.CurrentHP =
                    combatant.MaxHP =
                    combatant.CurrentMP =
                    combatant.MaxMP =
                    combatant.MaxTP =
                    combatant.CurrentTP = 0;
                    combatant.Statuses = new List<Status>();
                    combatant.Casting = new Cast();
                }
            }
            return combatant;
        }

        /// <summary>
        /// カレントターゲットの敵視情報を取得
        /// </summary>
        public unsafe List<EnmityEntry> GetEnmityEntryList()
        {
            short num = 0;
            uint topEnmity = 0;
            List<EnmityEntry> result = new List<EnmityEntry>();
            List<Combatant> combatantList = Combatants;
            Combatant mychar = GetSelfCombatant();

            /// 一度に全部読む
            byte[] buffer = GetByteArray(enmityAddress, 0x900 + 2);
            fixed (byte* p = buffer) num = (short)p[2296];

            if (num <= 0)
            {
                return result;
            }
            if (num > 31) num = 31; // changed??? 32->31

            for (short i = 0; i < num; i++)
            {
                int p = i * 72;
                uint _id;
                uint _enmity;

                fixed (byte* bp = buffer)
                {
                    _id = *(uint*)&bp[p + 56];
                    _enmity = *(uint*)&bp[p + 60];
                }
                var entry = new EnmityEntry()
                {
                    ID = _id,
                    Enmity = _enmity,
                    isMe = false,
                    Name = "Unknown",
                    Job = 0x00
                };
                if (entry.ID > 0)
                {
                    Combatant c = combatantList.Find(x => x.ID == entry.ID);
                    if (c != null)
                    {
                        entry.Name = c.Name;
                        entry.Job = c.Job;
                        entry.OwnerID = c.OwnerID;
                    }
                    if (entry.ID == mychar.ID)
                    {
                        entry.isMe = true;
                    }
                    if (topEnmity <= entry.Enmity)
                    {
                        topEnmity = entry.Enmity;
                    }
                    entry.HateRate = (int)(((double)entry.Enmity / (double)topEnmity) * 100);
                    result.Add(entry);
                }
            }
            return result;
        }

        /// <summary>
        /// 敵視リスト情報を取得
        /// </summary>
        public unsafe List<AggroEntry> GetAggroList()
        {
            int num = 0;
            uint currentTargetID = 0;
            List<AggroEntry> result = new List<AggroEntry>();
            List<Combatant> combatantList = Combatants;
            Combatant mychar = GetSelfCombatant();

            // 一度に全部読む
            byte[] buffer = GetByteArray(aggroAddress, 32 * 72 + 2);
            fixed (byte* p = buffer) num = (short)p[2296];
            if (num <= 0)
            {
                return result;
            }
            if (num > 31) num = 31; // max changed??? 32->31


            // current target
            //currentTargetID = GetUInt32(aggroAddress, -4);
            //if (currentTargetID == 3758096384u) currentTargetID = 0;
            var targetCombatant = GetTargetCombatant();
            if (targetCombatant != null)
            {
                currentTargetID = targetCombatant.ID;
            }
            else
            {
                currentTargetID = 0;
            }
            //
            for (int i = 0; i < num; i++)
            {
                int p = i * 72;
                uint _id;
                short _enmity;

                fixed (byte* bp = buffer)
                {
                    _id = *(uint*)&bp[p + 56];
                    _enmity = (short)bp[p + 60];
                }

                var entry = new AggroEntry()
                {
                    ID = _id,
                    HateRate = _enmity,
                    Name = "Unknown",
                };
                if (entry.ID <= 0) continue;
                Combatant c = combatantList.Find(x => x.ID == entry.ID);
                if (c != null)
                {
                    entry.ID = c.ID;
                    entry.Order = c.Order;
                    entry.isCurrentTarget = (c.ID == currentTargetID);
                    entry.Name = c.Name;
                    entry.MaxHP = c.MaxHP;
                    entry.CurrentHP = c.CurrentHP;
                    entry.Statuses = c.Statuses;
                    if (c.TargetID > 0)
                    {
                        Combatant t = combatantList.Find(x => x.ID == c.TargetID);
                        if (t != null)
                        {
                            entry.Target = new EnmityEntry()
                            {
                                ID = t.ID,
                                Name = t.Name,
                                Job = t.Job,
                                OwnerID = t.OwnerID,
                                isMe = mychar.ID == t.ID ? true : false,
                                Enmity = 0,
                                HateRate = 0
                            };
                        }
                    }
                }
                result.Add(entry);
            }
            return result;
        }

        /// <summary>
        /// バイト配列からUTF-8文字列に変換
        /// </summary>
        private static string GetStringFromBytes(byte[] source, int offset = 0, int size = 256)
        {
            var bytes = new byte[size];
            Array.Copy(source, offset, bytes, 0, size);
            var realSize = 0;
            for (var i = 0; i < size; i++)
            {
                if (bytes[i] != 0)
                {
                    continue;
                }
                realSize = i;
                break;
            }
            Array.Resize(ref bytes, realSize);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// バッファの長さだけメモリを読み取ってバッファに格納
        /// </summary>
        private bool Peek(IntPtr address, byte[] buffer)
        {
            IntPtr zero = IntPtr.Zero;
            IntPtr nSize = new IntPtr(buffer.Length);
            return NativeMethods.ReadProcessMemory(_process.Handle, address, buffer, nSize, ref zero);
        }

        /// <summary>
        /// メモリから指定された長さだけ読み取りバイト配列として返す
        /// </summary>
        /// <param name="address">読み取る開始アドレス</param>
        /// <param name="length">読み取る長さ</param>
        /// <returns></returns>
        private byte[] GetByteArray(IntPtr address, int length)
        {
            var data = new byte[length];
            Peek(address, data);
            return data;
        }

        /// <summary>
        /// メモリから4バイト読み取り32ビットIntegerとして返す
        /// </summary>
        /// <param name="address">読み取る位置</param>
        /// <param name="offset">オフセット</param>
        /// <returns></returns>
        private unsafe int GetInt32(IntPtr address, int offset = 0)
        {
            int ret;
            var value = new byte[4];
            Peek(IntPtr.Add(address, offset), value);
            fixed (byte* p = &value[0]) ret = *(int*)p;
            return ret;
        }

        /// <summary>
        /// </summary>
        /// <param name="address"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private unsafe uint GetUInt32(IntPtr address, int offset = 0)
        {
            uint ret;
            var value = new byte[4];
            Peek(IntPtr.Add(address, offset), value);
            fixed (byte* p = &value[0]) ret = *(uint*)p;
            return ret;
        }

        /// <summary>
        /// </summary>
        /// <param name="address"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private unsafe UInt64 GetUInt64(IntPtr address, int offset = 0)
        {
            UInt64 ret;
            var value = new byte[8];
            Peek(IntPtr.Add(address, offset), value);
            fixed (byte* p = &value[0]) ret = *(UInt64*)p;
            return ret;
        }

        /// <summary>
        /// Signature scan.
        /// Read data at address which follow matched with the pattern and return it as a pointer.
        /// </summary>
        /// <param name="pattern">byte pattern signature</param>
        /// <param name="offset">offset to read</param>
        /// <param name="bRIP">x64 rip relative addressing mode if true</param>
        /// <returns>the pointer addresses</returns>
        private List<IntPtr> SigScan(string pattern, int offset = 0, bool bRIP = false)
        {
            if (pattern == null || pattern.Length % 2 != 0)
            {
                return new List<IntPtr>();
            }

            // 1byte = 2char
            byte?[] patternByteArray = new byte?[pattern.Length / 2];

            // Convert Pattern to "Array of Byte"
            for (int i = 0; i < pattern.Length / 2; i++)
            {
                string text = pattern.Substring(i * 2, 2);
                if (text == "??")
                {
                    patternByteArray[i] = null;
                }
                else
                {
                    patternByteArray[i] = new byte?(Convert.ToByte(text, 16));
                }
            }

            int moduleMemorySize = _process.MainModule.ModuleMemorySize;
            IntPtr baseAddress = _process.MainModule.BaseAddress;
            IntPtr intPtr_EndOfModuleMemory = IntPtr.Add(baseAddress, moduleMemorySize);
            IntPtr intPtr_Scannning = baseAddress;

            int splitSizeOfMemory = 65536;
            byte[] splitMemoryArray = new byte[splitSizeOfMemory];

            List<IntPtr> list = new List<IntPtr>();

            // while loop for scan all memory
            while (intPtr_Scannning.ToInt64() < intPtr_EndOfModuleMemory.ToInt64())
            {
                IntPtr nSize = new IntPtr(splitSizeOfMemory);

                // if remaining memory size is less than splitSize, change nSize to remaining size
                if (IntPtr.Add(intPtr_Scannning, splitSizeOfMemory).ToInt64() > intPtr_EndOfModuleMemory.ToInt64())
                {
                    nSize = (IntPtr)(intPtr_EndOfModuleMemory.ToInt64() - intPtr_Scannning.ToInt64());
                }

                IntPtr intPtr_NumberOfBytesRead = IntPtr.Zero;

                // read memory
                if (NativeMethods.ReadProcessMemory(_process.Handle, intPtr_Scannning, splitMemoryArray, nSize, ref intPtr_NumberOfBytesRead))
                {
                    int num = 0;

                    // slide start point byte bu byte, check with patternByteArray
                    while ((long)num < intPtr_NumberOfBytesRead.ToInt64() - (long)patternByteArray.Length - (long)offset)
                    {
                        int matchCount = 0;
                        for (int j = 0; j < patternByteArray.Length; j++)
                        {
                            // pattern "??" have a null value. in this case, skip the check.
                            if (!patternByteArray[j].HasValue)
                            {
                                matchCount++;
                            }
                            else
                            {
                                if (patternByteArray[j].Value != splitMemoryArray[num + j])
                                {
                                    break;
                                }
                                matchCount++;
                            }
                        }

                        // if all bytes are match, it means "the pattern found"
                        if (matchCount == patternByteArray.Length)
                        {
                            IntPtr item;
                            if (bRIP)
                            {
                                item = new IntPtr(BitConverter.ToInt32(splitMemoryArray, num + patternByteArray.Length + offset));
                                item = new IntPtr(intPtr_Scannning.ToInt64() + (long)num + (long)patternByteArray.Length + 4L + item.ToInt64());
                            }
                            else if (_mode == FFXIVClientMode.FFXIV_64)
                            {
                                item = new IntPtr(BitConverter.ToInt64(splitMemoryArray, num + patternByteArray.Length + offset));
                                item = new IntPtr(item.ToInt64());
                            }
                            else
                            {
                                item = new IntPtr(BitConverter.ToInt32(splitMemoryArray, num + patternByteArray.Length + offset));
                                item = new IntPtr(item.ToInt64());
                            }

                            // add the item if not contains already
                            if (item != IntPtr.Zero && !list.Contains(item))
                            {
                                list.Add(item);
                            }
                        }
                        num++;
                    }
                }
                intPtr_Scannning = IntPtr.Add(intPtr_Scannning, splitSizeOfMemory - patternByteArray.Length - offset);
            }
            return list;
        }
    }
}
