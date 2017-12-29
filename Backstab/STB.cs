using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Backstab
{
    class STB
    {
        public static IEnumerable<string> GetLines(Stream stream)
        {
            var sjis = Encoding.GetEncoding("sjis");
            using (var br = new BinaryReader(stream))
            {
                br.ReadInt32();
                stream.Position = br.ReadInt32() + 0x38;

                var stk = new Stack<object>();
                while (true)
                {
                    var (opcode, sub, val) = (br.ReadInt32(), br.ReadInt32(), br.ReadInt32());
                    switch (opcode)
                    {
                        // opcodes that push something to stack
                        case 3:
                            if (sub == 1) stk.Push(val);
                            else if (sub == 2) stk.Push(BitConverter.ToSingle(BitConverter.GetBytes(val), 0));
                            else if (sub == 3) stk.Push(ReadString(val + 0x58));
                            else throw new NotSupportedException();
                            break;
                        case 6:
                            stk.Push(true);
                            break;
                        case 0xB:
                            stk.Push(null);
                            break;
                        case 0x13:
                            stk.Push((ushort)val);
                            break;
                        
                        // opcodes that produce a line of code
                        case 4:
                            var u = (ushort)stk.Pop();
                            if (u == 0) yield return $"time = {(int)stk.Pop()};";
                            else yield return $"macro(0x{u:X4});";
                            break;
                        case 0xF:
                            yield return $"exit({(int)stk.Pop()});";
                            break;
                        case 0x15:
                            var stk2 = new Stack<string>();
                            bool suffix = false;
                            while (stk2.Count != sub - 1)
                            {
                                var tmp = stk.Pop();
                                if (tmp == null)
                                {
                                    suffix = true;
                                    continue;
                                }
                                stk2.Push(Stringify(tmp) + (suffix ? "f" : ""));
                                suffix = false;
                            }

                            if (!stk.Pop().Equals(true)) throw new NotSupportedException();
                            var type = (int)stk.Pop();
                            if ((int)stk.Pop() != 64) throw new NotSupportedException();
                            yield return $"sub{type:000}({string.Join(", ", stk2)});";
                            break;
                        
                        // final opcode indicating end of program
                        case 0:
                            if (stk.Any()) throw new NotSupportedException();
                            yield break;

                        default:
                            throw new NotSupportedException();
                    }
                }

                // auxiliary functions for reading sjis-encoded null-terminated string
                string ReadString(int offset)
                {
                    var tmp = stream.Position;
                    stream.Position = offset;
                    var str = sjis.GetString(Enumerable.Range(0, 999).Select(_ => br.ReadByte()).TakeWhile(b => b != 0).ToArray());
                    stream.Position = tmp;
                    return str;
                }
            }
        }

        static string Stringify(object obj)
        {
            switch (obj)
            {
                case float f: return $"{f:R}{(f == (int)f ? ".0" : "")}";
                case int i: return $"{i}";
                case string s: return $"\"{s}\"";
                default: throw new NotSupportedException();
            }
        }
    }
}
