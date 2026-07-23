using System.Runtime.CompilerServices;

namespace MCFight
{
    /// <summary>
    /// 轻量 KV 存储，替代 web 版的 120 字段大 struct。
    /// 使用类（引用类型）避免 struct ref 限制。
    /// 最多 32 个键值对，足够任何单个 boss。
    /// </summary>
    public class SkillStateMap
    {
        private const int MAX_ENTRIES = 32;

        private int _count;
        private int _key0, _key1, _key2, _key3, _key4, _key5, _key6, _key7;
        private int _key8, _key9, _key10, _key11, _key12, _key13, _key14, _key15;
        private int _key16, _key17, _key18, _key19, _key20, _key21, _key22, _key23;
        private int _key24, _key25, _key26, _key27, _key28, _key29, _key30, _key31;

        private float _val0, _val1, _val2, _val3, _val4, _val5, _val6, _val7;
        private float _val8, _val9, _val10, _val11, _val12, _val13, _val14, _val15;
        private float _val16, _val17, _val18, _val19, _val20, _val21, _val22, _val23;
        private float _val24, _val25, _val26, _val27, _val28, _val29, _val30, _val31;

        public int Count => _count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref int KeyRef(int i)
        {
            switch (i)
            {
                case 0: return ref _key0; case 1: return ref _key1; case 2: return ref _key2; case 3: return ref _key3;
                case 4: return ref _key4; case 5: return ref _key5; case 6: return ref _key6; case 7: return ref _key7;
                case 8: return ref _key8; case 9: return ref _key9; case 10: return ref _key10; case 11: return ref _key11;
                case 12: return ref _key12; case 13: return ref _key13; case 14: return ref _key14; case 15: return ref _key15;
                case 16: return ref _key16; case 17: return ref _key17; case 18: return ref _key18; case 19: return ref _key19;
                case 20: return ref _key20; case 21: return ref _key21; case 22: return ref _key22; case 23: return ref _key23;
                case 24: return ref _key24; case 25: return ref _key25; case 26: return ref _key26; case 27: return ref _key27;
                case 28: return ref _key28; case 29: return ref _key29; case 30: return ref _key30; case 31: return ref _key31;
                default: return ref _key0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref float ValRef(int i)
        {
            switch (i)
            {
                case 0: return ref _val0; case 1: return ref _val1; case 2: return ref _val2; case 3: return ref _val3;
                case 4: return ref _val4; case 5: return ref _val5; case 6: return ref _val6; case 7: return ref _val7;
                case 8: return ref _val8; case 9: return ref _val9; case 10: return ref _val10; case 11: return ref _val11;
                case 12: return ref _val12; case 13: return ref _val13; case 14: return ref _val14; case 15: return ref _val15;
                case 16: return ref _val16; case 17: return ref _val17; case 18: return ref _val18; case 19: return ref _val19;
                case 20: return ref _val20; case 21: return ref _val21; case 22: return ref _val22; case 23: return ref _val23;
                case 24: return ref _val24; case 25: return ref _val25; case 26: return ref _val26; case 27: return ref _val27;
                case 28: return ref _val28; case 29: return ref _val29; case 30: return ref _val30; case 31: return ref _val31;
                default: return ref _val0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindIndex(int key)
        {
            for (int i = 0; i < _count; i++)
                if (KeyRef(i) == key) return i;
            return -1;
        }

        public void SetFloat(int key, float value)
        {
            int idx = FindIndex(key);
            if (idx >= 0) { ValRef(idx) = value; return; }
            if (_count < MAX_ENTRIES) { KeyRef(_count) = key; ValRef(_count) = value; _count++; }
        }

        public float GetFloat(int key, float defaultValue = 0f)
        {
            int idx = FindIndex(key);
            return idx >= 0 ? ValRef(idx) : defaultValue;
        }

        public void SetInt(int key, int value) => SetFloat(key, value);
        public int GetInt(int key, int defaultValue = 0) => (int)GetFloat(key, defaultValue);

        public void SetBool(int key, bool value) => SetFloat(key, value ? 1f : 0f);
        public bool GetBool(int key, bool defaultValue = false) => GetFloat(key, defaultValue ? 1f : 0f) != 0f;

        public bool Has(int key) => FindIndex(key) >= 0;

        public void Remove(int key)
        {
            int idx = FindIndex(key);
            if (idx < 0) return;
            // 交换末尾到删除位置
            _count--;
            if (idx != _count)
            {
                KeyRef(idx) = KeyRef(_count);
                ValRef(idx) = ValRef(_count);
            }
        }

        public void Clear() => _count = 0;
    }
}
