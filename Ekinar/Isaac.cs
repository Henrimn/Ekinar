namespace Ekinar
{
    class Isaac
    {
        uint count;
        uint[] rsl = new uint[256];
        uint[] mm = new uint[256];
        uint aa, bb, cc;

		public Isaac()
        {
            Init(false);
        }
		
        public Isaac(uint seed)
        {
            seed *= 9U;
            rsl[0] = seed;
            rsl[seed % 64U] = seed;
            Init(true);
        }

        public byte Next()
        {
            if (count == 0)
            {
                Generate();
                count = 256;
            }
            count--;
            return (byte)rsl[count];
        }

        public void Generate()
        {
            uint i, x, y;
            cc++;
            bb += cc;

            for (i = 0; i <= 255; i++)
            {
                x = mm[i];
                switch (i & 3)
                {
                    case 0: aa = aa ^ (aa << 13); break;
                    case 1: aa = aa ^ (aa >> 6); break;
                    case 2: aa = aa ^ (aa << 2); break;
                    case 3: aa = aa ^ (aa >> 16); break;
                }
                aa = mm[(i + 128) & 255] + aa;
                y = mm[(x >> 2) & 255] + aa + bb;
                mm[i] = y;
                bb = mm[(y >> 10) & 255] + x;
                rsl[i] = bb;
            }
        }

        void Mix(ref uint a, ref uint b, ref uint c, ref uint d, ref uint e, ref uint f, ref uint g, ref uint h)
        {
            a = a ^ b << 11; d += a; b += c;
            b = b ^ c >> 2; e += b; c += d;
            c = c ^ d << 8; f += c; d += e;
            d = d ^ e >> 16; g += d; e += f;
            e = e ^ f << 10; h += e; f += g;
            f = f ^ g >> 4; a += f; g += h;
            g = g ^ h << 8; b += g; h += a;
            h = h ^ a >> 9; c += h; a += b;
        }

        void Init(bool flag)
        {
            short i; uint a, b, c, d, e, f, g, h;

            aa = 0; bb = 0; cc = 0;
            a = 0x9e3779b9;
            b = a; c = a; d = a;
            e = a; f = a; g = a; h = a;

            for (i = 0; i <= 3; i++)
                Mix(ref a, ref b, ref c, ref d, ref e, ref f, ref g, ref h);

            i = 0;
            do
            {
                if (flag)
                {
                    a += rsl[i]; b += rsl[i + 1]; c += rsl[i + 2]; d += rsl[i + 3];
                    e += rsl[i + 4]; f += rsl[i + 5]; g += rsl[i + 6]; h += rsl[i + 7];
                }

                Mix(ref a, ref b, ref c, ref d, ref e, ref f, ref g, ref h);
                mm[i] = a; mm[i + 1] = b; mm[i + 2] = c; mm[i + 3] = d;
                mm[i + 4] = e; mm[i + 5] = f; mm[i + 6] = g; mm[i + 7] = h;
                i += 8;
            }
            while (i < 255);

            if (flag)
            {
                i = 0;
                do
                {
                    a += mm[i]; b += mm[i + 1]; c += mm[i + 2]; d += mm[i + 3];
                    e += mm[i + 4]; f += mm[i + 5]; g += mm[i + 6]; h += mm[i + 7];
                    Mix(ref a, ref b, ref c, ref d, ref e, ref f, ref g, ref h);
                    mm[i] = a; mm[i + 1] = b; mm[i + 2] = c; mm[i + 3] = d;
                    mm[i + 4] = e; mm[i + 5] = f; mm[i + 6] = g; mm[i + 7] = h;
                    i += 8;
                }
                while (i < 255);
            }
            Generate();
            count = 256;
        }
    }
}
