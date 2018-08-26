using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using Helper = Neo.SmartContract.Framework.Helper;
using System.Numerics;

namespace Neo.NeunitySerialization
{
    public static class Op
    {
        public static byte[] Void() => new byte[0];

        public static BigInteger Bytes2BigInt(byte[] data) => data.AsBigInteger();

        public static byte[] BigInt2Bytes(BigInteger bigInteger) => bigInteger.AsByteArray();



        public static byte[] String2Bytes(String str) => str.AsByteArray();

        public static String Bytes2String(byte[] data) => data.AsString();

        public static String BigInt2String(BigInteger bigInteger) => bigInteger.AsByteArray().AsString();

        public static bool Bytes2Bool(byte[] data)
        {
            if (data.Length == 0) return false;
            return data[0] != 0;
        }

        public static byte[] Bool2Bytes(bool val)
        {
            if (val) return new byte[1] { 1 };
            return new byte[1] { 0 };
        }

        public static byte Bytes2Byte(byte[] data) => data[0];

        public static byte[] Byte2Bytes(byte b) => new byte[1] { b };

        public static byte Int2Byte(int i) => (byte)i;

        //public static int BigInt2Int(BigInteger i) => (int)i;

        public static byte[] SubBytes(byte[] data, int start, int length) => Helper.Range(data, start, length);


        public static byte[] JoinTwoByteArray(byte[] ba1, byte[] ba2) => ba1.Concat(ba2);

        public static byte[] JoinByteArray(params byte[][] args)
        {
            if (args.Length == 0) { return new byte[0]; }
            else
            {
                byte[] r = args[0];
                for (int i = 1; i < args.Length; i++)
                {
                    r = JoinTwoByteArray(r, args[i]);
                }

                return r;
            }
        }

        public static bool And(bool left, bool right)
        {
            if (left) return right;
            return false;
        }

        public static bool Or(bool left, bool right)
        {
            if (left) return true;
            return right;
        }


        public static void Log(params object[] ba)
        {
            Runtime.Notify(ba);
        }


        public static void SetStoragePath(string path)
        {

        }

        //public static byte[] RandomSeed(){
        //    Blockchain.GetTransaction()
        //}
    }


    public static class NuSD
    {

        /* -----------------------------------------------------------------
         * Seralization/Deseralization
         * 
         * 
         * 
         *      Body   =====Seg=====>    Segment      ===Join===> Table (combination of segs without further pre.)
         *    [body]   <====DeSeg====    [pre][body] <===Dequeue==== [pre1][body1][pre2][[pre2.1][body2.1][pre2.2][body2.2]]
         *                     
         * 
         * "Pre" represents body's length
         -----------------------------------------------------------------*/

        #region Length
        public static int PreLenOfBody(byte[] body) => body.Length / 255 + 1;
        public static int SegLenOfBody(byte[] body) => PreLenOfBody(body) + body.Length;

        public static int PreLenOfSeg(byte[] segment) => PreLenOfFirstSegFromTable(segment); //A table can be just one segment
        public static int PreLenOfFirstSegFromTable(byte[] table) => PreLenOfFirstSegFromData(table, 0);
        public static int PreLenOfFirstSegFromData(byte[] data, int segStartIndex)
        {
            int i = 0;
            while (data[segStartIndex + i++] == 255) { }
            return i;
        }

        public static int BodyLenOfSeg(byte[] segment) => BodyLenOfFirstSegFromTable(segment);
        public static int BodyLenOfFirstSegFromTable(byte[] table) => BodyLenOfFirstSegFromData(table, 0);
        public static int BodyLenOfFirstSegFromData(byte[] data, int segStartIndex)
        {
            int prelen = PreLenOfFirstSegFromData(data, segStartIndex);
            return (prelen - 1) * 255 + data[prelen + segStartIndex - 1];
        }

        public static int SegLenOfFirstSegFromTable(byte[] table) => SegLenOfFirstSegFromData(table, 0);
        public static int SegLenOfFirstSegFromData(byte[] data, int segStartIndex)
        {
            int prelen = PreLenOfFirstSegFromData(data, segStartIndex);
            return prelen + (prelen - 1) * 255 + data[prelen + segStartIndex - 1];
        }


        #endregion

        #region Counts

        public static int NumSegsOfTable(byte[] table) => NumSegsOfTableFromData(table, 0);
        public static int NumSegsOfSeg(byte[] segment) => NumSegsOfSegFromData(segment, 0);
        public static int NumSegsOfSegFromData(byte[] data, int segStartIndex = 0) => NumSegsOfTableFromData(data, PreLenOfFirstSegFromData(data, segStartIndex) + segStartIndex);

        public static int NumSegsOfTableFromData(byte[] data, int tblStartIndex = 0)
        {
            int i = tblStartIndex;
            int r = 0;
            while (i < data.Length)
            {
                i += SegLenOfFirstSegFromData(data, i);
                ++r;
            }
            return r;
        }


        #endregion

        #region Seg

        public static byte[] SegInt(BigInteger body) => Seg(Op.BigInt2Bytes(body));
        public static byte[] SegString(string body) => Seg(Op.String2Bytes(body));
        public static byte[] SegBool(bool body) => Seg(Op.Bool2Bytes(body));
        //public static byte[] SegByte(byte b) => new byte[2] { 1, b };

        public static byte[] Seg(byte[] body)
        {   //if body = Op.Void: rem = 0;

            BigInteger rem = (body.Length) % 255;

            byte[] r = Op.Void();

            for (int i = 0; i < body.Length / 255; i++)
            {
                r = Op.JoinTwoByteArray(r, new byte[1] { 255 });
            }

            byte[] remBA = Op.BigInt2Bytes(rem);
            if (rem >= 128 && rem < 256)
            {
                remBA = Op.SubBytes(remBA, 0, 1);
            }
            return Op.JoinByteArray(r, remBA, body);
        }


        #endregion

        #region Join
        public static byte[] JoinSegs2Seg(params byte[][] segs) => Seg(JoinSegs2Table(segs));

        public static byte[] JoinSegs2Table(params byte[][] segs) => Op.JoinByteArray(segs);

        public static byte[] JoinToTable(byte[] table, byte[] seg) => Op.JoinTwoByteArray(table, seg);

        #endregion

        #region Deseg

        public static byte[] Deseg(byte[] seg) => DesegFromTable(seg);   //Seg is a table with only 1 seg
        public static byte[] DesegFromTable(byte[] table) => DesegFromTableFromData(table, 0);

        public static byte[] DesegFromTableFromData(byte[] data, int start)
        {
            if (start >= data.Length)
            {
                return Op.Void();
            }
            else
            {
                int i = 0;  //prefixLength -1
                int segLen = 0;
                while (data[i + start] == 255)
                {
                    segLen += 255;
                    ++i;
                }
                segLen += data[i + start];
                if (segLen == 0)
                {
                    return Op.Void();
                }
                else
                {
                    return Op.SubBytes(data, start + i + 1, segLen);
                }
            }


        }


        //Get the value (as byte[]) with the variable id w/o having all previous segments deseged.
        public static byte[] DesegWithIdFromTable(byte[] table, int id) => DesegWithIdFromData(table, 0, id);
        public static byte[] DesegWithIdFromSeg(byte[] segment, int id) => DesegWithIdFromData(segment, PreLenOfSeg(segment), id);

        public static byte[] DesegWithIdFromData(byte[] data, int tblStartIndex, int id)
        {
            int i = 0;
            int preStart = tblStartIndex;
            while (i < id)
            {
                preStart += SegLenOfFirstSegFromData(data, preStart);
                if (preStart > data.Length)
                {
                    return Op.Void();
                }
                ++i;
            }
            return DesegFromTableFromData(data, preStart);

        }
        #endregion

        #region Standard

        public static byte[] AddSeg(this byte[] data, byte[] body) => Op.JoinTwoByteArray(data, Seg(body));
        public static byte[] AddSegInt(this byte[] data, BigInteger body) => Op.JoinTwoByteArray(data, SegInt(body));
        public static byte[] AddSegBool(this byte[] data, bool body) => Op.JoinTwoByteArray(data, SegBool(body));
        public static byte[] AddSegStr(this byte[] data, string body) => Op.JoinTwoByteArray(data, SegString(body));

        public static byte[] AddBody(this byte[] data, byte[] body) => Op.JoinTwoByteArray(data, body);
        public static byte[] AddBodyInt(this byte[] data, BigInteger body) => Op.JoinTwoByteArray(data, SegInt(body));
        public static byte[] AddBodyBool(this byte[] data, bool body) => Op.JoinTwoByteArray(data, SegBool(body));
        public static byte[] AddBodyStr(this byte[] data, string body) => Op.JoinTwoByteArray(data, SegString(body));

        public static byte[] SplitSeg(this byte[] data, int startID) => DesegFromTableFromData(data, startID);
        public static BigInteger SplitSegInt(this byte[] data, int startID) => Op.Bytes2BigInt(DesegFromTableFromData(data, startID));
        public static bool SplitSegBool(this byte[] data, int startID) => Op.Bytes2Bool(DesegFromTableFromData(data, startID));
        public static string SplitSegStr(this byte[] data, int startID) => Op.Bytes2String(DesegFromTableFromData(data, startID));

        public static byte[] SplitBody(this byte[] data, int startID, int length) => Op.SubBytes(data, startID, length);

        public static byte[] SplitTbl(this byte[] table, int index) => DesegWithIdFromData(table, 0, index);
        public static BigInteger SplitTblInt(this byte[] table, int index) => Op.Bytes2BigInt(DesegWithIdFromData(table, 0, index));
        public static bool SplitTblBool(this byte[] table, int index) => Op.Bytes2Bool(DesegWithIdFromData(table, 0, index));
        public static string SplitTblStr(this byte[] table, int index) => Op.Bytes2String(DesegWithIdFromData(table, 0, index));
        public static int SizeTable(this byte[] table) => NumSegsOfTableFromData(table, 0);

        #endregion
    }

    /** The Utilities */
    public static class NuIO
    {
        /** The result state of Storage.Put Operation */
        public static class State
        {
            public const byte Create = 0;
            public const byte Update = 1;
            public const byte Delete = 2;
            public const byte Unchanged = 3;
            public const byte Abort = 4;
            public const byte Invalid = 99;
        }

        /* ===========================================================
        * Storage functions are designed to support multi-segment keys
        * Eg. {Key = "seg1.seg2.seg3", Value = "someValue"}
        ==============================================================*/

        public static byte[] KeyPath(params string[] elements)
        {
            if (elements.Length == 0)
            {
                return new byte[0];
            }
            else
            {
                string r = "";
                for (int i = 0; i < elements.Length; i++)
                {
                    r = r + "/" + elements[i];
                }
                return Op.String2Bytes(r);
            }
        }

        public static byte[] KeyPath(byte[] splitter, params string[] elements)
        {
            if (elements.Length == 0)
            {
                return new byte[0];
            }
            else
            {
                byte[] r = Op.String2Bytes(elements[0]);
                for (int i = 1; i < elements.Length; i++)
                {
                    r = Op.JoinByteArray(r, splitter, Op.String2Bytes(elements[i]));
                }
                return r;
            }
        }
        public static byte[] GetStorageWithKeyPath(params string[] elements) => GetStorageWithKey(KeyPath(elements));

        public static byte[] GetStorageWithKey(byte[] key) => Storage.Get(Storage.CurrentContext, key);

        public static byte[] GetStorageWithKey(string key) => Storage.Get(Storage.CurrentContext, key);


        public static byte SetStorageWithKeyPath(byte[] value, params string[] segments)
        {
            return SetStorageWithKey(KeyPath(segments), value);
        }

        public static byte SetStorageWithKey(string key, byte[] value)
        {
            // To avoid repeat spend of GAS caused by unchanged storage
            byte[] orig = GetStorageWithKey(key);
            if (orig == value) { return State.Unchanged; }

            if (value.Length == 0)
            {
                Storage.Delete(Storage.CurrentContext, key);
                return State.Delete;

            }
            else
            {
                Storage.Put(Storage.CurrentContext, key, value);
                return (orig.Length == 0) ? State.Create : State.Update;
            }
        }


        public static byte SetStorageWithKey(byte[] key, byte[] value)
        {
            if (value.Length == 0)
            {
                Storage.Delete(Storage.CurrentContext, key);
                return State.Delete;
            }
            else
            {
                byte[] orig = GetStorageWithKey(key);
                if (orig == value) { return State.Unchanged; }
                else
                {
                    Storage.Put(Storage.CurrentContext, key, value);
                    return (orig.Length == 0) ? State.Create : State.Update;
                }

            }
        }

    }

    public class PlayCard
    {
        public enum CardSuit
        {
            Spades = 1 << 0,
            Hearts = 1 << 1,
            Diamonds = 1 << 2,
            Clubs = 1 << 3
        }

        public enum CardValue
        {
            Ace = 1,
            Two = 2, Three = 3, Four = 4, Five = 5, Six = 6, Seven = 7, Eight = 8, Nine = 9, Ten = 10,
            Jack = 11,
            Queen = 12,
            King = 13
        }

        public static BigInteger CardSuit2BigInteger(CardSuit suit)
        {
            BigInteger result;
            result = (BigInteger)(int)suit;
            return result;
        }

        public static CardSuit BigInteger2Suit(BigInteger value)
        {
            int intValue = (int)value;

            return (CardSuit)intValue;
        }

        public static BigInteger CardValue2BigInteger(CardValue value)
        {
            BigInteger result;
            result = (BigInteger)(int)value;
            return result;
        }

        public static CardValue BigInteger2Value(BigInteger value)
        {
            int intValue = (int)value;

            return (CardValue)intValue;
        }

        // 
        // NuSD Definition of Card: 
        // <Card> = [S<id>,S<name>,S<birthBlock>,S<level>,S<ownerId>,S<isFighting#1>]
        //
        public class Card
        {
            public byte[] id;
            public CardSuit suit;
            public CardValue value;
            public bool isPlayed;
        }

    }

    public class PlayCardContract: SmartContract.Framework.SmartContract
    {
        #region NuSD: Neunity Serialization
        public static byte[] Card2Bytes(PlayCard.Card card)
        {
            if (card == null)
            {
                return Op.Void();
            }
            else
            {
                BigInteger suit = PlayCard.CardSuit2BigInteger(card.suit);
                BigInteger value = PlayCard.CardValue2BigInteger(card.value);
                return NuSD.Seg(card.id)
                           .AddSegInt(suit)
                           .AddSegInt(value)
                           .AddSegBool(card.isPlayed);
            }
        }

        public static PlayCard.Card Bytes2Card(byte[] data)
        {
            if (data.Length == 0) return null;

            PlayCard.Card card = new PlayCard.Card
            {
                id = data.SplitTbl(0),
                suit = PlayCard.BigInteger2Suit(data.SplitTblInt(1)),
                value = PlayCard.BigInteger2Value(data.SplitTblInt(2)),
                isPlayed = data.SplitTblBool(3)
            };

            return card;
        }
        #endregion

        public static Object Main(string operation, params object[] args)
        {
            if (args.Length > 0)
            {
                if (operation == "create")
                {
                    byte[] id = (byte[])args[0];
                    BigInteger suit = (BigInteger)args[1];
                    BigInteger value = (BigInteger)args[2];
                    bool isPlayed = (bool)args[3];

                    PlayCard.Card card = new PlayCard.Card
                    {
                        id = id,
                        suit = PlayCard.BigInteger2Suit(suit),
                        value = PlayCard.BigInteger2Value(value),
                        isPlayed = isPlayed
                    };

                    SaveCard(card);
                    return card;
                }
                if (operation == "get")
                {
                    byte[] id = (byte[])args[0];
                    return ReadCard(id);
                }
                return false;
            }
            return false;
        }

        static byte SaveCard(PlayCard.Card card)
        {
            return NuIO.SetStorageWithKeyPath(Card2Bytes(card), "c", Op.Bytes2String(card.id));
        }

        static PlayCard.Card ReadCard(byte[] id)
        {
            byte[] data = NuIO.GetStorageWithKeyPath("c", Op.Bytes2String(id));
            return Bytes2Card(data);
        }
    }
}