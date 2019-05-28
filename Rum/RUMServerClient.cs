using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using MessagePack;
using com.fpnn;

namespace com.rum
{
    public delegate void CallbackDelegate(Exception exception);

    public class RUMServerClient
    {
        private static class IdGenerator {
            static private long Count = 0;
            static private Object Lock = new Object();

            static public long Gen() {
                long c = 0;
                lock(Lock)
                {
                    if (++Count >= 999)
                        Count = 0;
                    c = Count;
                }
                return Convert.ToInt64(Convert.ToString(FPCommon.GetMilliTimestamp()) + Convert.ToString(c));
            }
        }

        private int Pid;
        private string Secret;
        private string Host;
        private int Port;
        private bool AutoReconnect;
        private int Timeout;
        private FPClient Client;

        private Object SaltLock = new Object();

        private string Rid = null;
        private long Sid = 0;

        public ConnectedCallbackDelegate ConnectedCallback {
            get { return Client.ConnectedCallback; }
            set {
                Client.ConnectedCallback = value;
            }
        }

        public ClosedCallbackDelegate ClosedCallback {
            get { return Client.ClosedCallback; }
            set {
                Client.ClosedCallback = value;
            }
        }

        public ErrorCallbackDelegate ErrorCallback {
            get { return Client.ErrorCallback; }
            set {
                Client.ErrorCallback = value;
            }
        }

        public RUMServerClient(int pid, string secret, string host, int port, bool reconnect, int timeout)
        {
            Pid = pid;
            Secret = secret;
            Host = host;
            Port = port;
            AutoReconnect = reconnect;
            Timeout = timeout;
            Init();
        }

        public void SetRumId(string rid)
        {
            Rid = rid;
        }

        public void SetSessionId(long sid)
        {
            Sid = sid;
        }

        private void Init()
        {
            Client = new FPClient(Host, Port, AutoReconnect, Timeout);
        }

        public void Connect()
        {
            Client.Connect();
        }

        public void Reconnect()
        {
            Client.Reconnect();
        }

        public void Close()
        {
            Client.Close();
        }

        private string CalcMd5(string str, bool upper)
        {
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(str);
            return CalcMd5(inputBytes, upper);
        }

        private string CalcMd5(byte[] bytes, bool upper)
        {
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] hash = md5.ComputeHash(bytes);
            string f = "x2";
            if (upper)
                f = "X2";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString(f));
            }
            return sb.ToString();
        }

        private Hashtable GenRumEventPayload(string eventName, Hashtable attrs) 
        {
            if (Sid == 0)
                Sid = IdGenerator.Gen();
            if (Rid == null)
                Rid = Convert.ToString(IdGenerator.Gen());

            Hashtable ev = new Hashtable();
            ev["ev"] = eventName;
            ev["sid"] = Sid;
            ev["rid"] = Rid;
            ev["ts"] = FPCommon.GetTimestamp();
            ev["eid"] = IdGenerator.Gen();
            ev["source"] = "csharp";
            ev["attrs"] = attrs;
            return ev;
        }

        private FPData GenRumData(string eventName, Hashtable attrs)
        {
            ArrayList events = new ArrayList();
            events.Add(GenRumEventPayload(eventName, attrs));
            return GenRumData(events);
        }

        private FPData GenRumData(ArrayList eventList) {
            ArrayList events = new ArrayList();
            foreach (Hashtable ev in eventList)
            {
                if (ev.ContainsKey("ev") && ev.ContainsKey("attrs"))
                    events.Add(GenRumEventPayload((string)ev["ev"], (Hashtable)ev["attrs"]));
            }

            if (events.Count == 0)
                return null;

            int salt = FPCommon.GetTimestamp();
            string sign = CalcMd5(Convert.ToString(Pid) + ":" + Secret + ":" + Convert.ToString(salt), true);
            Hashtable mp = new Hashtable();
            mp["pid"] = Pid;
            mp["salt"] = salt;
            mp["sign"] = sign;
            mp["events"] = events;
            byte[] payload = MessagePackSerializer.Serialize<Hashtable>(mp);

            FPData data = new FPData();
            data.SetFlag(FP_FLAG.FP_FLAG_MSGPACK);
            data.SetMType(FP_MSG_TYPE.FP_MT_TWOWAY);
            data.SetMethod("adds");
            data.SetPayload(payload);
            return data;
        }

        private Exception CheckException(Hashtable mp)
        {
            if (mp.ContainsKey("ex") && mp.ContainsKey("code"))
            {
                int errorCode = Convert.ToInt32(mp["code"]);
                if (errorCode > 0 && errorCode <= 30002)
                    Reconnect();

                string errorMsg = Convert.ToString(errorCode) + " : " + mp["ex"];
                return new Exception(errorMsg);
            }
            return null;
        }

        public void SendRumQuest(FPData data, int timeout, CallbackDelegate cb)
        {
            Client.SendQuest(data, (CallbackData cbd) =>
            {
                if (cbd.Exception == null)
                {
                    Hashtable result = MessagePackSerializer.Deserialize<Hashtable>(cbd.Data.payload);
                    Exception ex = CheckException(result);
                    if (ex == null)
                        cb(null);
                    else
                        cb(ex);
                }
                else
                    cb(cbd.Exception);
            }, timeout);
        }

        public void SendCustomEvent(string eventName, Hashtable attrs, int timeout, CallbackDelegate cb)
        {
            FPData data = GenRumData(eventName, attrs);
            if (data == null)
            {
                cb(new Exception("Param Error"));
                return;
            }
            SendRumQuest(data, timeout, cb);
        }

        public void SendCustomEvents(ArrayList events, int timeout, CallbackDelegate cb) 
        {
            FPData data = GenRumData(events);
            if (data == null)
            {
                cb(new Exception("Param Error"));
                return;
            }
            SendRumQuest(data, timeout, cb);
        }


    }
}
