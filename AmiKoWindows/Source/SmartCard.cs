using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Generic;
using System.Text;

using PCSC;
using PCSC.Iso7816;
using System.Globalization;
using PCSC.Monitoring;

namespace AmiKoWindows
{
    class SmartCard
    {
        private const byte INS_ERASE_BIN = 0x0E;
        private const byte INS_VRFY = 0x20;
        private const byte INS_MANAGE_CHANNEL = 0x70;
        private const byte INS_EXT_AUTH = 0x82;
        private const byte INS_GET_CHALLENGE = 0x84;
        private const byte INS_SELECT_FILE = 0xA4;
        private const byte INS_READ_BIN = 0xB0;
        private const byte INS_READ_REC = 0xB2;
        private const byte INS_GET_RESP = 0xC0;
        private const byte INS_ENVELOPE = 0xC2;
        private const byte INS_GET_DATA = 0xCA;
        private const byte INS_WRITE_BIN = 0xD0;
        private const byte INS_WRITE_REC = 0xD2;
        private const byte INS_UPDATE_BIN = 0xD6;
        private const byte INS_PUT_DATA = 0xDA;
        private const byte INS_UPDATE_REC = 0xDC;
        private const byte INS_APPEND_REC = 0xE2;
        /* for our transaction tracking, not defined in the specification */
        private const byte INS_INVALID = 0x00;

        public static readonly SmartCard Instance = new SmartCard();
        private bool DeviceMonitoring = false;
        private bool Monitoring = false;
        private IDeviceMonitor DeviceMonitor = null;
        private ISCardMonitor Monitor = null;

        public delegate void ReceivedCardResultHandler(object sender, Result r);
        public event ReceivedCardResultHandler ReceivedCardResult;

        private SmartCard() { }
        ~SmartCard()
        {
            this.DeviceMonitor.StatusChanged -= DeviceMonitor_StatusChanged;
            this.DeviceMonitor.Cancel();
            this.Monitor.CardInserted -= Monitor_CardInserted;
            this.Monitor.Cancel();
        }

        public void StartMonitor()
        {
            if (this.DeviceMonitoring) return;
            DeviceMonitor = DeviceMonitorFactory.Instance.Create(SCardScope.System);
            DeviceMonitor.StatusChanged += DeviceMonitor_StatusChanged;
            DeviceMonitor.Initialized += (sender, args) => Console.WriteLine("5: {0}", args.ToString());
            DeviceMonitor.MonitorException += (sender, args) => Console.WriteLine("6: {0}", args.ToString());

            Monitor = MonitorFactory.Instance.Create(SCardScope.System);
            Monitor.CardInserted += Monitor_CardInserted;
            Monitor.CardInserted += (sender, args) => Console.WriteLine("0: {0}", args.ToString());
            Monitor.CardRemoved += (sender, args) => Console.WriteLine("1: {0}", args.ToString());
            Monitor.Initialized += (sender, args) => Console.WriteLine("2: {0}", args.ToString());
            Monitor.StatusChanged += (sender, args) => Console.WriteLine("3: {0}", args.ToString());
            Monitor.MonitorException += (sender, args) => Console.WriteLine("4: {0}", args.ToString());

            DeviceMonitor.Start();
            this.DeviceMonitoring = true;
            this.InitialMonitorReaders();
        }

        private void DeviceMonitor_StatusChanged(object sender, DeviceChangeEventArgs e)
        {
            if (this.Monitoring)
            {
                Monitor.Cancel();
            }
            var readers = e.AllReaders.ToArray();
            Monitor.Start(readers);
            this.Monitoring = false;
        }

        private void Monitor_CardInserted(object sender, CardStatusEventArgs e)
        {
            this.RunAndRaise(e.ReaderName);
        }

        private void InitialMonitorReaders()
        {
            using (var context = ContextFactory.Instance.Establish(SCardScope.System))
            {
                var readers = context.GetReaders();
                Monitor.Start(readers);
                Monitoring = true;

                foreach (var readerName in readers)
                {
                    this.RunAndRaise(readerName);
                }
            }
        }

        public void RunAndRaise(string readerName)
        {
            Result r = new Result();
            try
            {
                this.Run(readerName, r);
                if (this.ReceivedCardResult != null)
                {
                    this.ReceivedCardResult(this, r);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Monitor_CardInserted exception: {0}", ex.ToString());
            }
        }

        public void Run(string readerName, Result r)
        {
            var contextFactory = ContextFactory.Instance;
            using (var context = contextFactory.Establish(SCardScope.System))
            {
                var status = context.GetReaderStatus(readerName);
                var reader = new SCardReader(context);
                reader.Connect(readerName, SCardShareMode.Shared, SCardProtocol.T1);
                reader.GetAttrib(SCardAttribute.AtrString, null, out var atrLength);
                var cardAtr = new byte[atrLength];
                reader.GetAttrib(SCardAttribute.AtrString, out cardAtr);
                byte[] correctBytes = {
                    0x3b, 0x9f, 0x13, 0x81, 0xb1, 0x80, 0x37, 0x1f,
                    0x03, 0x80, 0x31, 0xf8, 0x69, 0x4d, 0x54, 0x43,
                    0x4f, 0x53, 0x70, 0x02, 0x01, 0x02, 0x81, 0x07, 0x86
                };
                Console.WriteLine("ATR: {0}", BitConverter.ToString(cardAtr));
                if (!cardAtr.SequenceEqual(correctBytes))
                {
                    return;
                }
                var isoReader = new IsoReader(reader);
                byte[] ef_id = { 0x2f, 0x06 };
                selectFile(isoReader, ef_id);
                readBinary(isoReader, 84, r);

                byte[] ef_ad = { 0x2f, 0x07 };
                selectFile(isoReader, ef_ad);
                readBinary(isoReader, 95, r);
            }
        }

        private void selectFile(IsoReader reader, byte[] filePath)
        {
            var command = new CommandApdu(IsoCase.Case4Extended, SCardProtocol.T1);
            command.INS = INS_SELECT_FILE;
            command.P1 = 0x08;
            command.P2 = 0x00;
            command.Data = filePath;
            command.Le = 0;
            var response = reader.Transmit(command);
            var sw = response.SW1;

            Console.WriteLine("ok");
        }

        private void readBinary(IsoReader reader, int expectedResponseLength, Result r)
        {
            var command = new CommandApdu(IsoCase.Case2Extended, SCardProtocol.T1);
            command.INS = INS_READ_BIN;
            command.Le = expectedResponseLength; // The length of the expected response
            var response = reader.Transmit(command);
            var data = response.GetData();
            parseCardData(data, r);
        }

        private void parseCardData(byte[] data, Result r)
        {
            int offset = 0;

            byte packetType = data[0];
            byte packetSize = data[1];

            byte[] payload = data.Skip(2).Take(packetSize).ToArray();
            switch (packetType)
            {
                case 0x65:
                    while (offset < packetSize)
                    {
                        offset += this.parseTLV(payload.Skip(offset).Take(packetSize - offset).ToArray(), r);
                    }
                    break;

                default:
                    break;
            }
        }

        private int parseTLV(byte[] data, Result r)
        {
            byte tag = data[0];
            byte length = data[1];
            byte[] value = data.Skip(2).Take(length).ToArray();
            switch (tag)
            {
                case 0x80:  // UTF8InternationalString
                    {
                        // Name
                        string s = Encoding.UTF8.GetString(value);
                        string[] a = s.Split(new string[] { ", " }, StringSplitOptions.None);
                        string familyName = a[0];
                        r.FamilyName = familyName;
                        if (a.Length > 1)
                        {
                            string givenName = a[1];
                            r.GivenName = givenName;
                        }
                    }
                    break;

                case 0x82:  // NUMERIC STRING
                    {
                        // Date of Birth
                        string s = Encoding.UTF8.GetString(value);
                        CultureInfo provider = CultureInfo.InvariantCulture;

                        try
                        {
                            var result = DateTime.ParseExact(s, "yyyyMMdd", provider);
                            string birthDate = result.ToString("dd.MM.yyyy");
                            Console.WriteLine("birthdate {0}", birthDate);
                            r.BirthDate = birthDate;
                        }
                        catch (FormatException e)
                        {
                            Console.WriteLine("no {0}", e.ToString());
                        }
                    }
                    break;

                case 0x83:  // UTF8InternationalString
                    {
                        string s = Encoding.UTF8.GetString(value);
                        Console.WriteLine("Card holder ID: {0}", s);
                    }
                    break;

                case 0x84:  // ENUMERATED
                    {
                        byte sexEnum = value[0];
                        if (sexEnum == 1)
                        {
                            r.Gender = @"man";
                        }
                        else if (sexEnum == 2)
                        {
                            r.Gender = @"woman";
                        }
                    }
                    break;

                case 0x90: // UTF8InternationalString
                    {
                        string s = Encoding.UTF8.GetString(value);
                        Console.WriteLine("Issuing State ID Number <{0}>", s);
                    }
                    break;

                case 0x91: // UTF8InternationalString
                    {
                        string s = Encoding.UTF8.GetString(value);
                        Console.WriteLine("name Of The Institution <{0}>", s);
                    }
                    break;

                case 0x92:  // NUMERIC STRING
                    {
                        string s = Encoding.UTF8.GetString(value);
                        Console.WriteLine("identificationNumber Of The Institution <{0}>", s);
                    }
                    break;

                case 0x93: // UTF8InternationalString
                    {
                        string s = Encoding.UTF8.GetString(value);
                        Console.WriteLine("Insured Person Number <{0}>", s);
                    }
                    break;

                case 0x94:  // NUMERIC STRING
                    {
                        string s = Encoding.UTF8.GetString(value);
                        Console.WriteLine("ExpiryDate yyyyMMdd <{0}>", s);
                    }
                    break;

                default:
                    Console.WriteLine("T:0x{0:X}, L:{1:D}, V: <{2:X}>", tag, length, value);
                    break;
            }
            return length + 2;
        }

        public class Result
        {
            public string FamilyName = null;
            public string GivenName = null;
            public string BirthDate = null;
            public string Gender = null;

            public Result() { }
        }
    }
}
