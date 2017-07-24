﻿using System.Collections.Generic;
using System.Linq;
using Pk3DSRNGTool.RNG;

namespace Pk3DSRNGTool
{
    public class TinyTimeline
    {
        #region subclass
        private class TinyCall
        {
            public int frame;
            public int type;
            public TinyCall(int f, int t)
            {
                frame = f;
                type = t;
            }
        }

        private class TinyCallList
        {
            private List<TinyCall> list = new List<TinyCall>();

            public void Addfront(int f, int t) => list.Add(new TinyCall(f, t));

            public void Add(int f, int t)
            {
                if (t < 0) return;
                list.Add(new TinyCall(f, t));
                list = list.OrderByDescending(e => e.frame).ToList();
            }

            public TinyCall First()
            {
                var tmp = new TinyCall(list.Last().frame, list.Last().type);
                list.RemoveAt(list.Count - 1);
                return tmp;
            }
        }
        #endregion

        public const int Buffer = 100;

        public int Startingframe;
        public int Maxframe;
        public byte Method;
        public int Parameter;
        private TinyCallList Status = new TinyCallList();
        public TinyMT Tinyrng;

        public int TinyLength => results.Count - Buffer;
        public void Add(int f, int t) => Status.Add(f, t);
        public uint getrand => Tinyrng.Nextuint();
        public uint[] State => (uint[])Tinyrng.status.Clone();

        public List<Frame_Tiny> results;
        public Frame_Tiny FindFrame(int frame) => results?.FirstOrDefault(t => t.framemin < frame && frame <= t.framemax);

        public void Generate()
        {
            int Currentframe = Startingframe - 2;
            int i = 0;
            results = new List<Frame_Tiny>();
            while (Currentframe <= Maxframe)
            {
                var newdata = new Frame_Tiny();
                newdata.Index = i++;
                newdata.state = State;
                newdata.framemin = Currentframe;
                var call = Status.First();
                Currentframe = newdata.framemax = call.frame;
                newdata.rand = Tinyrng.Nextuint();
                switch (call.type)
                {
                    case 0:
                        Status.Addfront(Currentframe, newdata.rand < 0x55555556 ? 1 : 2);
                        break;
                    case 1:
                        Status.Add(Currentframe + getcooldown2(newdata.rand), 2);
                        break;
                    case 2:
                        Status.Add(Currentframe + getcooldown1(newdata.rand), 0);
                        break;
                    case 3:
                        Status.Add(Currentframe + getcooldown3(newdata.rand), 3);
                        break;
                    case 4:
                        Status.Add(Currentframe + 180, 4);
                        break;
                }
                results.Add(newdata);
            }
            int imax = i + Buffer;
            for (; i < imax; i++)
            {
                var newdata = new Frame_Tiny();
                newdata.Index = i;
                newdata.state = State;
                newdata.framemin = Currentframe;
                newdata.framemax = Currentframe;
                newdata.rand = Tinyrng.Nextuint();
                results.Add(newdata);
            }
            switch (Method)
            {
                case 0: MarkFS(); break;
                case 2: MarkSync(); break;
            }
        }

        private int PM_Num => Parameter;
        private int SlotNum => Parameter;

        public void MarkSync()
        {
            int delay1 = 3 * PM_Num;
            int delay2 = 4 * PM_Num;
            int max = results.Count - delay2;
            for (int i = 0; i < max; i++)
            {
                results[i]._sync = true;
                for (int j = i + delay1; j <= i + delay2; j++)
                {
                    if (results[j].rand > 0x7FFFFFFF)
                    {
                        results[i]._sync = false;
                        break;
                    }
                }
            }
        }

        public void MarkFS()
        {
            int max = results.Count - 2;
            for (int i = 0; i < max; i++)
            {
                if (results[i + 1].Rand100 < 13)
                    results[i]._fs = (byte)(((results[i + 2].rand * SlotNum) >> 32) + 1);
            }
        }

        private static int getcooldown1(uint rand) => (int)((((rand * 60ul) >> 32) * 2 + 124));
        private static int getcooldown2(uint rand) => rand < 0x55555556 ? 20 : 12;
        private static int getcooldown3(uint rand) => (int)((((rand * 90ul) >> 32) * 2 + 720));
    }
}
