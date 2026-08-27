using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media;
using RuriLib.LS;
using RuriLib.Models;

namespace RuriLib;

// Alphabetical order matching OpenBullet's list
public enum DnsRecordType { A, AAAA, CAA, CNAME, MX, NAPTR, NS, PTR, SOA, SRV, TLSA, TXT, URI }
public enum DnsTransport  { UDP, TCP, DnsOverHttps }

public class BlockDns : BlockBase
{
    private string        query        = "";
    private DnsRecordType recordType   = DnsRecordType.A;
    private DnsTransport  transport    = DnsTransport.UDP;
    private string        server       = "";
    private int           timeoutMs    = 10000;
    private string        variableName = "";
    private bool          isCapture    = false;

    public string VariableName
    {
        get => variableName;
        set { variableName = value; OnPropertyChanged("VariableName"); }
    }

    public bool IsCapture
    {
        get => isCapture;
        set { isCapture = value; OnPropertyChanged("IsCapture"); }
    }

    public string Query
    {
        get => query;
        set { query = value; OnPropertyChanged("Query"); }
    }

    public DnsRecordType RecordType
    {
        get => recordType;
        set { recordType = value; OnPropertyChanged("RecordType"); }
    }

    public DnsTransport Transport
    {
        get => transport;
        set { transport = value; OnPropertyChanged("Transport"); }
    }

    public string Server
    {
        get => server;
        set { server = value; OnPropertyChanged("Server"); }
    }

    public int TimeoutMs
    {
        get => timeoutMs;
        set { timeoutMs = value; OnPropertyChanged("TimeoutMs"); }
    }

    public BlockDns() { Label = "DNS Lookup"; }

    // ─── LoliScript deserialization ──────────────────────────────────────────
    public override BlockBase FromLS(string line)
    {
        string input = line.Trim();
        if (input.StartsWith("#")) Label = LineParser.ParseLabel(ref input);

        Query      = LineParser.ParseLiteral(ref input, "Query");
        RecordType = LineParser.ParseEnum(ref input, "RecordType", typeof(DnsRecordType));
        Transport  = LineParser.ParseEnum(ref input, "Transport",  typeof(DnsTransport));
        Server     = LineParser.ParseLiteral(ref input, "Server");

        if (LineParser.Lookahead(ref input) == TokenType.Integer)
            TimeoutMs = LineParser.ParseInt(ref input, "Timeout");

        if (LineParser.ParseToken(ref input, TokenType.Arrow, essential: false) == string.Empty)
            return this;

        string varType = LineParser.ParseToken(ref input, TokenType.Parameter, essential: false);
        IsCapture    = varType.ToUpper() == "CAP";
        VariableName = LineParser.ParseToken(ref input, TokenType.Literal, essential: false);
        return this;
    }

    // ─── LoliScript serialization ────────────────────────────────────────────
    public override string ToLS(bool indent = true)
    {
        var w = new BlockWriter(GetType(), indent, Disabled);
        w.Label(Label).Token("DNS")
         .Literal(Query)
         .Token(RecordType)
         .Token(Transport)
         .Literal(Server)
         .Integer(TimeoutMs);

        if (!string.IsNullOrEmpty(VariableName))
            w.Arrow().Token(IsCapture ? "CAP" : "LIST").Literal(VariableName);

        return w.ToString();
    }

    // ─── Execution ───────────────────────────────────────────────────────────
    public override void Process(BotData data)
    {
        base.Process(data);

        string q   = ReplaceValues(Query,  data);
        string srv = ReplaceValues(Server, data);

        // Accept full URLs — extract just the hostname
        if ((q.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
             q.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            && Uri.TryCreate(q, UriKind.Absolute, out var uri))
            q = uri.Host;

        string via = string.IsNullOrWhiteSpace(srv) ? Transport.ToString() : srv;
        data.Log(new LogEntry($"Queried {RecordType} records for {q} via {via}", Colors.White));
        data.LogNewLine();

        List<string> results;
        try
        {
            results = ResolveAsync(q, RecordType, Transport, srv, TimeoutMs).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            data.Log(new LogEntry("Error: " + ex.Message, Colors.Tomato));
            results = new List<string>();
        }

        if (results.Count == 0)
            data.Log(new LogEntry("No records found", Colors.Orange));
        else
            foreach (var r in results)
                data.Log(new LogEntry(r, Colors.LightSkyBlue));

        if (!string.IsNullOrEmpty(VariableName))
            InsertVariable(data, IsCapture, results, VariableName, "", "");
    }

    // ─── Dispatcher ──────────────────────────────────────────────────────────

    private static async Task<List<string>> ResolveAsync(
        string query, DnsRecordType type, DnsTransport transport, string server, int timeoutMs)
    {
        string host = ParseServerHost(server, out int port);
        byte[] q;
        byte[] resp;

        switch (transport)
        {
            case DnsTransport.UDP:
                q    = BuildDnsQuery(query, type);
                resp = await QueryUdpAsync(host, port, q, timeoutMs);
                // TC bit set → truncated, RFC 1035 says retry over TCP
                if (resp.Length >= 3 && (resp[2] & 0x02) != 0)
                    resp = await QueryTcpAsync(host, port, q, timeoutMs);
                return ParseDnsWireResponse(resp, type);

            case DnsTransport.TCP:
                q    = BuildDnsQuery(query, type);
                resp = await QueryTcpAsync(host, port, q, timeoutMs);
                return ParseDnsWireResponse(resp, type);

            case DnsTransport.DnsOverHttps:
                return await QueryDoHAsync(query, type, server, timeoutMs);

            default:
                return new List<string>();
        }
    }

    // Parses "1.1.1.1", "1.1.1.1:5353" from Server field; default server is 8.8.8.8.
    private static string ParseServerHost(string server, out int port)
    {
        port = 53;
        string host = string.IsNullOrWhiteSpace(server) ? "8.8.8.8" : server.Trim();
        int colon = host.LastIndexOf(':');
        if (colon > 0 && int.TryParse(host.Substring(colon + 1), out int p))
        {
            port = p;
            host = host.Substring(0, colon);
        }
        return host;
    }

    // ─── UDP ─────────────────────────────────────────────────────────────────

    private static async Task<byte[]> QueryUdpAsync(string server, int port, byte[] query, int timeoutMs)
    {
        using var udp = new UdpClient();
        udp.Connect(server, port);
        await udp.SendAsync(query, query.Length);
        var recv = udp.ReceiveAsync();
        if (await Task.WhenAny(recv, Task.Delay(timeoutMs)) != recv)
            throw new TimeoutException("DNS UDP query timed out");
        return (await recv).Buffer;
    }

    // ─── TCP ─────────────────────────────────────────────────────────────────

    private static async Task<byte[]> QueryTcpAsync(string server, int port, byte[] query, int timeoutMs)
    {
        using var tcp = new TcpClient();
        var conn = tcp.ConnectAsync(server, port);
        if (await Task.WhenAny(conn, Task.Delay(timeoutMs)) != conn)
            throw new TimeoutException("DNS TCP connect timed out");
        await conn;

        using var stream = tcp.GetStream();
        stream.ReadTimeout  = timeoutMs;
        stream.WriteTimeout = timeoutMs;

        // RFC 1035 §4.2.2: TCP DNS messages are prefixed with a 2-byte big-endian length
        var msg = new byte[2 + query.Length];
        msg[0] = (byte)(query.Length >> 8);
        msg[1] = (byte)(query.Length & 0xFF);
        Array.Copy(query, 0, msg, 2, query.Length);
        await stream.WriteAsync(msg, 0, msg.Length);

        var lenBuf = new byte[2];
        await ReadExactAsync(stream, lenBuf, 2);
        int respLen = (lenBuf[0] << 8) | lenBuf[1];
        var resp    = new byte[respLen];
        await ReadExactAsync(stream, resp, respLen);
        return resp;
    }

    private static async Task ReadExactAsync(NetworkStream stream, byte[] buffer, int count)
    {
        int read = 0;
        while (read < count)
        {
            int n = await stream.ReadAsync(buffer, read, count - read);
            if (n == 0) throw new EndOfStreamException("DNS TCP: connection closed prematurely");
            read += n;
        }
    }

    // ─── DNS wire format — query builder ─────────────────────────────────────

    private static byte[] BuildDnsQuery(string domain, DnsRecordType type)
    {
        var buf = new List<byte>(64);
        ushort id = (ushort)(Environment.TickCount & 0xFFFF);
        // Header
        buf.Add((byte)(id >> 8));  buf.Add((byte)(id & 0xFF));
        buf.Add(0x01);             buf.Add(0x00); // flags: RD=1
        buf.Add(0x00);             buf.Add(0x01); // QDCOUNT=1
        buf.Add(0x00);             buf.Add(0x00); // ANCOUNT=0
        buf.Add(0x00);             buf.Add(0x00); // NSCOUNT=0
        buf.Add(0x00);             buf.Add(0x00); // ARCOUNT=0
        // QNAME: each label is length-prefixed, terminated by 0x00
        foreach (string label in domain.TrimEnd('.').Split('.'))
        {
            byte[] lb = Encoding.ASCII.GetBytes(label);
            buf.Add((byte)lb.Length);
            buf.AddRange(lb);
        }
        buf.Add(0x00);
        // QTYPE
        ushort qt = DnsTypeCode(type);
        buf.Add((byte)(qt >> 8)); buf.Add((byte)(qt & 0xFF));
        // QCLASS: IN=1
        buf.Add(0x00); buf.Add(0x01);
        return buf.ToArray();
    }

    private static ushort DnsTypeCode(DnsRecordType t) => t switch
    {
        DnsRecordType.A     =>   1,
        DnsRecordType.NS    =>   2,
        DnsRecordType.CNAME =>   5,
        DnsRecordType.SOA   =>   6,
        DnsRecordType.PTR   =>  12,
        DnsRecordType.MX    =>  15,
        DnsRecordType.TXT   =>  16,
        DnsRecordType.AAAA  =>  28,
        DnsRecordType.SRV   =>  33,
        DnsRecordType.NAPTR =>  35,
        DnsRecordType.TLSA  =>  52,
        DnsRecordType.URI   => 256,
        DnsRecordType.CAA   => 257,
        _                   =>   1,
    };

    // ─── DNS wire format — response parser ───────────────────────────────────

    private static List<string> ParseDnsWireResponse(byte[] msg, DnsRecordType type)
    {
        var results = new List<string>();
        if (msg.Length < 12) return results;

        int anCount = (msg[6] << 8) | msg[7];
        if (anCount == 0) return results;

        // Skip 12-byte header then the question section
        int pos = 12;
        pos = SkipName(msg, pos); // QNAME
        pos += 4;                 // QTYPE + QCLASS

        ushort qtype = DnsTypeCode(type);
        for (int i = 0; i < anCount && pos < msg.Length; i++)
        {
            pos = SkipName(msg, pos); // NAME
            if (pos + 10 > msg.Length) break;

            ushort rtype = (ushort)((msg[pos] << 8) | msg[pos + 1]); pos += 2;
            pos += 2; // CLASS
            pos += 4; // TTL
            int rdLen = (msg[pos] << 8) | msg[pos + 1]; pos += 2;
            int rdEnd = pos + rdLen;

            if (rtype == qtype)
            {
                string value = ParseRData(msg, pos, rdLen, rtype);
                if (!string.IsNullOrEmpty(value)) results.Add(value);
            }

            pos = rdEnd;
        }
        return results;
    }

    private static string ParseRData(byte[] msg, int pos, int rdLen, ushort rtype)
    {
        int end = pos + rdLen;
        switch (rtype)
        {
            case 1: // A — 4 bytes IPv4
                if (rdLen == 4)
                    return new IPAddress(new[] { msg[pos], msg[pos+1], msg[pos+2], msg[pos+3] }).ToString();
                break;

            case 28: // AAAA — 16 bytes IPv6
                if (rdLen == 16)
                {
                    var b16 = new byte[16];
                    Array.Copy(msg, pos, b16, 0, 16);
                    return new IPAddress(b16).ToString();
                }
                break;

            case 2:  // NS
            case 5:  // CNAME
            case 12: // PTR
                return ReadName(msg, pos).TrimEnd('.');

            case 15: // MX: 2-byte preference followed by exchange name
                return ReadName(msg, pos + 2).TrimEnd('.');

            case 16: // TXT: one or more <len><data> strings concatenated
            {
                var sb = new StringBuilder();
                int p  = pos;
                while (p < end)
                {
                    int slen = msg[p++];
                    sb.Append(Encoding.UTF8.GetString(msg, p, Math.Min(slen, end - p)));
                    p += slen;
                }
                return sb.ToString();
            }

            case 6: // SOA: primary name server (mname)
                return ReadName(msg, pos).TrimEnd('.');

            case 33: // SRV: priority weight port target
                if (rdLen >= 6)
                {
                    int priority = (msg[pos]   << 8) | msg[pos+1];
                    int weight   = (msg[pos+2] << 8) | msg[pos+3];
                    int srvPort  = (msg[pos+4] << 8) | msg[pos+5];
                    return $"{priority} {weight} {srvPort} {ReadName(msg, pos + 6).TrimEnd('.')}";
                }
                break;

            case 35: // NAPTR: order preference flags service regexp replacement
                if (rdLen >= 4)
                {
                    int order  = (msg[pos] << 8) | msg[pos+1];
                    int pref   = (msg[pos+2] << 8) | msg[pos+3];
                    int p      = pos + 4;
                    string flags   = ReadLenString(msg, ref p, end);
                    string service = ReadLenString(msg, ref p, end);
                    string regexp  = ReadLenString(msg, ref p, end);
                    string repl    = p < end ? ReadName(msg, p).TrimEnd('.') : ".";
                    return $"{order} {pref} \"{flags}\" \"{service}\" \"{regexp}\" {repl}";
                }
                break;

            case 52: // TLSA: usage selector matching-type cert-data(hex)
                if (rdLen >= 3)
                {
                    int usage   = msg[pos];
                    int sel     = msg[pos+1];
                    int mtype   = msg[pos+2];
                    string hex  = BitConverter.ToString(msg, pos+3, end - (pos+3)).Replace("-", "").ToLower();
                    return $"{usage} {sel} {mtype} {hex}";
                }
                break;

            case 256: // URI: priority weight target
                if (rdLen >= 4)
                {
                    int priority = (msg[pos]   << 8) | msg[pos+1];
                    int weight   = (msg[pos+2] << 8) | msg[pos+3];
                    string target = Encoding.UTF8.GetString(msg, pos+4, end - (pos+4));
                    return $"{priority} {weight} \"{target}\"";
                }
                break;

            case 257: // CAA: flags tag value
                if (rdLen >= 2)
                {
                    int    flags  = msg[pos];
                    int    tagLen = msg[pos+1];
                    string tag    = Encoding.ASCII.GetString(msg, pos+2, Math.Min(tagLen, end - (pos+2)));
                    string val    = Encoding.UTF8.GetString(msg, pos+2+tagLen, end - (pos+2+tagLen));
                    return $"{flags} {tag} \"{val}\"";
                }
                break;
        }
        return null;
    }

    // Reads a length-prefixed ASCII string (used in NAPTR RDATA).
    private static string ReadLenString(byte[] msg, ref int pos, int end)
    {
        if (pos >= end) return "";
        int len = msg[pos++];
        len = Math.Min(len, end - pos);
        string s = Encoding.ASCII.GetString(msg, pos, len);
        pos += len;
        return s;
    }

    // Advances past a DNS name (handles compression pointers), returns next position.
    private static int SkipName(byte[] msg, int pos)
    {
        int safety = 0;
        while (pos < msg.Length && safety++ < 128)
        {
            byte b = msg[pos];
            if (b == 0)              return pos + 1; // end of name
            if ((b & 0xC0) == 0xC0) return pos + 2; // compression pointer (2 bytes)
            pos += 1 + b;                            // regular label
        }
        return pos;
    }

    // Reads a DNS name following compression pointers, returns dotted string.
    private static string ReadName(byte[] msg, int pos)
    {
        var sb     = new StringBuilder();
        int safety = 0;
        while (pos < msg.Length && safety++ < 128)
        {
            byte b = msg[pos];
            if (b == 0) break;
            if ((b & 0xC0) == 0xC0)
            {
                pos = ((b & 0x3F) << 8) | msg[pos + 1];
                continue;
            }
            if (sb.Length > 0) sb.Append('.');
            sb.Append(Encoding.ASCII.GetString(msg, pos + 1, b));
            pos += 1 + b;
        }
        return sb.ToString();
    }

    // ─── DoH ─────────────────────────────────────────────────────────────────

    private static async Task<List<string>> QueryDoHAsync(
        string query, DnsRecordType type, string server, int timeoutMs)
    {
        string baseUrl = string.IsNullOrWhiteSpace(server)
            ? "https://dns.google/resolve"
            : server.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? server.TrimEnd('/')
                : $"https://{server}/resolve";

        string url = $"{baseUrl}?name={Uri.EscapeDataString(query)}&type={type}";

        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(timeoutMs) };
        http.DefaultRequestHeaders.TryAddWithoutValidation("accept", "application/dns-json");

        string json = await http.GetStringAsync(url);
        return ParseDoHResponse(json, type);
    }

    private static List<string> ParseDoHResponse(string json, DnsRecordType type)
    {
        var results = new List<string>();
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("Answer", out var answers))
            return results;

        foreach (var answer in answers.EnumerateArray())
        {
            if (!answer.TryGetProperty("data", out var dataEl)) continue;
            string data = dataEl.GetString() ?? "";

            // Clean up trailing dots and quotes for types that have them
            if (type == DnsRecordType.MX && data.Contains(' '))
                data = data.Split(' ').Last().TrimEnd('.');
            else if (type == DnsRecordType.TXT)
                data = data.Trim('"');
            else if (type is DnsRecordType.PTR or DnsRecordType.CNAME or DnsRecordType.NS
                          or DnsRecordType.SOA)
                data = data.TrimEnd('.');

            if (!string.IsNullOrEmpty(data))
                results.Add(data);
        }
        return results;
    }
}
