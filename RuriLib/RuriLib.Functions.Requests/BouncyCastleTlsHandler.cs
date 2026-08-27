using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Buffers;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using ZstdSharp;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;
using Org.BouncyCastle.Security;

namespace RuriLib.Functions.Requests;

// ─────────────────────────────────────────────────────────────────────────────
// GREASE helper (RFC 8701) — Chrome inserts random reserved values so parsers
// don't reject unknown extension types / cipher suites / named groups.
// ─────────────────────────────────────────────────────────────────────────────
internal static class GreaseHelper
{
    private static readonly int[] Values =
    {
        0x0A0A, 0x1A1A, 0x2A2A, 0x3A3A, 0x4A4A, 0x5A5A, 0x6A6A, 0x7A7A,
        0x8A8A, 0x9A9A, 0xAAAA, 0xBABA, 0xCACA, 0xDADA, 0xEAEA, 0xFAFA
    };

    public static int Pick() => Values[Random.Shared.Next(Values.Length)];

    // Prepend a GREASE cipher suite at position 0
    public static int[] PrependCipher(int[] suites, int g)
    {
        var r = new int[suites.Length + 1];
        r[0] = g;
        Array.Copy(suites, 0, r, 1, suites.Length);
        return r;
    }

    // Prepend GREASE entry to key_share extension bytes.
    // ClientHello key_share format: [2B total_entries_len][entries...]
    // Each entry: [2B group][2B key_exchange_len][key_exchange bytes]
    public static byte[] PrependKeyShare(byte[] data, int g)
    {
        if (data == null || data.Length < 2) return data;
        int origLen = (data[0] << 8) | data[1];
        byte[] entry = { (byte)(g >> 8), (byte)(g & 0xFF), 0x00, 0x01, 0x00 }; // 5 bytes
        int newLen = origLen + entry.Length;
        var r = new byte[2 + newLen];
        r[0] = (byte)(newLen >> 8); r[1] = (byte)(newLen & 0xFF);
        Array.Copy(entry, 0, r, 2, entry.Length);
        Array.Copy(data, 2, r, 2 + entry.Length, origLen);
        return r;
    }

    // Prepend GREASE version to supported_versions extension bytes.
    // Client format: [1B total_bytes][2B per version...]
    public static byte[] PrependSupportedVersion(byte[] data, int g)
    {
        if (data == null || data.Length < 1) return data;
        int origCount = data[0];
        var r = new byte[3 + origCount];
        r[0] = (byte)(origCount + 2);
        r[1] = (byte)(g >> 8); r[2] = (byte)(g & 0xFF);
        Array.Copy(data, 1, r, 3, origCount);
        return r;
    }

    // Prepend GREASE named group to supported_groups extension bytes.
    // Format: [2B total_bytes][2B per group...]
    public static byte[] PrependNamedGroup(byte[] data, int g)
    {
        if (data == null || data.Length < 2) return data;
        int origLen = (data[0] << 8) | data[1];
        int newLen = origLen + 2;
        var r = new byte[2 + newLen];
        r[0] = (byte)(newLen >> 8); r[1] = (byte)(newLen & 0xFF);
        r[2] = (byte)(g >> 8); r[3] = (byte)(g & 0xFF);
        Array.Copy(data, 2, r, 4, origLen);
        return r;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// IDictionary<int,byte[]> that guarantees insertion order.
// Dictionary<> is NOT contractually ordered — bucket layout can change on resize.
// TLS extension order is part of the JA3/JA4 fingerprint, so order must be exact.
// ─────────────────────────────────────────────────────────────────────────────
internal sealed class InsertionOrderDictionary : IDictionary<int, byte[]>
{
    private readonly List<int> _order = new();
    private readonly Dictionary<int, byte[]> _map  = new();

    public byte[] this[int key]
    {
        get => _map[key];
        set { if (!_map.ContainsKey(key)) _order.Add(key); _map[key] = value; }
    }

    public ICollection<int>    Keys   => _order;
    public ICollection<byte[]> Values => _order.Select(k => _map[k]).ToList();
    public int  Count      => _map.Count;
    public bool IsReadOnly => false;

    public bool ContainsKey(int key)                       => _map.ContainsKey(key);
    public bool TryGetValue(int key, out byte[] value)     => _map.TryGetValue(key, out value);
    public void Add(int key, byte[] value)                 { _map.Add(key, value); _order.Add(key); }
    public bool Remove(int key)                            { _order.Remove(key); return _map.Remove(key); }
    public void Clear()                                    { _map.Clear(); _order.Clear(); }
    public void Add(KeyValuePair<int, byte[]> item)        => Add(item.Key, item.Value);
    public bool Contains(KeyValuePair<int, byte[]> item)   => _map.TryGetValue(item.Key, out var v) && v == item.Value;
    public bool Remove(KeyValuePair<int, byte[]> item)     => Remove(item.Key);
    public void CopyTo(KeyValuePair<int, byte[]>[] a, int i) { foreach (var k in _order) a[i++] = new(k, _map[k]); }

    public IEnumerator<KeyValuePair<int, byte[]>> GetEnumerator()
        => _order.Select(k => new KeyValuePair<int, byte[]>(k, _map[k])).GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

// ─────────────────────────────────────────────────────────────────────────────
// Shared TLS helpers — one BcTlsCrypto instance + shared ALPN builder
// ─────────────────────────────────────────────────────────────────────────────
internal static class TlsHelpers
{
    // Thread-safe; share across connections to avoid per-connection entropy seeding.
    internal static readonly BcTlsCrypto SharedCrypto = new BcTlsCrypto(new SecureRandom());

    // ALPN raw bytes: [2B list_len][[1B proto_len][proto_bytes]...]
    internal static byte[] BuildAlpn(params string[] protos)
    {
        using var body = new MemoryStream();
        foreach (var p in protos) { var b = Encoding.ASCII.GetBytes(p); body.WriteByte((byte)b.Length); body.Write(b, 0, b.Length); }
        var bd = body.ToArray();
        using var ms = new MemoryStream();
        ms.WriteByte((byte)(bd.Length >> 8)); ms.WriteByte((byte)(bd.Length & 0xFF));
        ms.Write(bd, 0, bd.Length);
        return ms.ToArray();
    }

    // Cached ALPN bytes for "h2, http/1.1" — result is always identical, compute once.
    internal static readonly byte[] AlpnH2Http11 = BuildAlpn("h2", "http/1.1");

    // Single authoritative implementation — called by both Http2Client and BouncyCastleTlsHandler.
    // Content-Encoding lists encodings in application order (first applied = leftmost).
    // To decompress we must undo them in reverse: rightmost first.
    internal static async Task<byte[]> DecompressAsync(byte[] data, string enc)
    {
        if (data.Length == 0 || string.IsNullOrEmpty(enc)) return data;
        var encodings = enc.Split(',');
        byte[] result = data;
        for (int i = encodings.Length - 1; i >= 0; i--)
        {
            var e = encodings[i].Trim();
            if (e.Equals("br", StringComparison.OrdinalIgnoreCase))
            { using var s = new MemoryStream(result); using var br = new BrotliStream(s, CompressionMode.Decompress); using var o = new MemoryStream(); await br.CopyToAsync(o); result = o.ToArray(); }
            else if (e.Equals("gzip", StringComparison.OrdinalIgnoreCase) || e.Equals("x-gzip", StringComparison.OrdinalIgnoreCase))
            { using var s = new MemoryStream(result); using var gz = new GZipStream(s, CompressionMode.Decompress); using var o = new MemoryStream(); await gz.CopyToAsync(o); result = o.ToArray(); }
            else if (e.Equals("deflate", StringComparison.OrdinalIgnoreCase))
            { using var s = new MemoryStream(result); using var df = new DeflateStream(s, CompressionMode.Decompress); using var o = new MemoryStream(); await df.CopyToAsync(o); result = o.ToArray(); }
            else if (e.Equals("zstd", StringComparison.OrdinalIgnoreCase))
            { using var s = new MemoryStream(result); using var zs = new DecompressionStream(s); using var o = new MemoryStream(); await zs.CopyToAsync(o); result = o.ToArray(); }
        }
        return result;
    }

    // Overload for H2 body accumulation — avoids an extra ToArray() copy when there is no encoding.
    internal static async Task<byte[]> DecompressAsync(MemoryStream source, string enc)
    {
        if (string.IsNullOrEmpty(enc)) return source.ToArray();
        source.Position = 0;
        var encodings = enc.Split(',');
        // For the first pass use the MemoryStream directly as the source
        byte[] result = null;
        for (int i = encodings.Length - 1; i >= 0; i--)
        {
            var e = encodings[i].Trim();
            using var tempInput = result == null ? null : new MemoryStream(result);
            Stream inputStream = tempInput ?? (Stream)source;
            using var o = new MemoryStream();
            if (e.Equals("br", StringComparison.OrdinalIgnoreCase))
            { using var br = new BrotliStream(inputStream, CompressionMode.Decompress); await br.CopyToAsync(o); }
            else if (e.Equals("gzip", StringComparison.OrdinalIgnoreCase) || e.Equals("x-gzip", StringComparison.OrdinalIgnoreCase))
            { using var gz = new GZipStream(inputStream, CompressionMode.Decompress); await gz.CopyToAsync(o); }
            else if (e.Equals("deflate", StringComparison.OrdinalIgnoreCase))
            { using var df = new DeflateStream(inputStream, CompressionMode.Decompress); await df.CopyToAsync(o); }
            else if (e.Equals("zstd", StringComparison.OrdinalIgnoreCase))
            { using var zs = new DecompressionStream(inputStream); await zs.CopyToAsync(o); }
            else { result = result ?? source.ToArray(); continue; }
            result = o.ToArray();
        }
        return result ?? source.ToArray();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Certificate authentication — validates chain+hostname when ignoreCert is false
// ─────────────────────────────────────────────────────────────────────────────
internal sealed class BrowserTlsAuthentication : TlsAuthentication
{
    private readonly bool _ignoreCert;
    private readonly string _host;

    public BrowserTlsAuthentication(bool ignoreCert, string host)
    {
        _ignoreCert = ignoreCert;
        _host = host;
    }

    public TlsCredentials GetClientCredentials(Org.BouncyCastle.Tls.CertificateRequest req) => null;

    public void NotifyServerCertificate(TlsServerCertificate serverCertificate)
    {
        if (_ignoreCert) return;
        var chain = serverCertificate.Certificate?.GetCertificateList();
        if (chain == null || chain.Length == 0)
            throw new TlsFatalAlert(AlertDescription.bad_certificate);
        try
        {
            using var leaf = new X509Certificate2(chain[0].GetEncoded());
            using var x509chain = new X509Chain();
            x509chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            var extras = new List<X509Certificate2>();
            try
            {
                for (int i = 1; i < chain.Length; i++)
                {
                    var c = new X509Certificate2(chain[i].GetEncoded());
                    extras.Add(c);
                    x509chain.ChainPolicy.ExtraStore.Add(c);
                }
                if (!x509chain.Build(leaf))
                    throw new TlsFatalAlert(AlertDescription.bad_certificate);
                if (!string.IsNullOrEmpty(_host) && !MatchesHostname(leaf, _host))
                    throw new TlsFatalAlert(AlertDescription.bad_certificate);
            }
            finally { foreach (var c in extras) c.Dispose(); }
        }
        catch (TlsFatalAlert) { throw; }
        catch { throw new TlsFatalAlert(AlertDescription.bad_certificate); }
    }

    private static bool MatchesHostname(X509Certificate2 cert, string host)
    {
        foreach (var ext in cert.Extensions)
        {
            if (ext.Oid?.Value != "2.5.29.17") continue; // SubjectAltName OID
            // Parse via BouncyCastle ASN.1 — locale-independent (ext.Format() output varies by OS language)
            var names = GeneralNames.GetInstance(Asn1Object.FromByteArray(ext.RawData));
            foreach (var gn in names.GetNames())
            {
                if (gn.TagNo != GeneralName.DnsName) continue;
                if (WildcardMatch(gn.Name.ToString(), host)) return true;
            }
            return false; // SAN present but no DNS entry matched
        }
        return WildcardMatch(cert.GetNameInfo(X509NameType.SimpleName, false), host);
    }

    private static bool WildcardMatch(string pattern, string host)
    {
        if (string.Equals(pattern, host, StringComparison.OrdinalIgnoreCase)) return true;
        if (!pattern.StartsWith("*.")) return false;
        int dot = host.IndexOf('.');
        return dot > 0 && host[dot..].Equals(pattern[1..], StringComparison.OrdinalIgnoreCase);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Chrome TLS client — GREASE in cipher suites, named groups, key_share,
// supported_versions; Chrome 120+ extension order; ALPN h2+http/1.1
// ─────────────────────────────────────────────────────────────────────────────
internal sealed class ChromeTlsClient : DefaultTlsClient
{
    // Chrome 131+ signature algorithms in order (source: tls.peet.ws + Wireshark captures)
    private static readonly (byte Hash, byte Sig)[] ChromeSigAlgs =
    {
        (4, 3), // ecdsa_secp256r1_sha256
        (8, 4), // rsa_pss_rsae_sha256
        (4, 1), // rsa_pkcs1_sha256
        (5, 3), // ecdsa_secp384r1_sha384
        (8, 5), // rsa_pss_rsae_sha384
        (5, 1), // rsa_pkcs1_sha384
        (8, 6), // rsa_pss_rsae_sha512
        (6, 1), // rsa_pkcs1_sha512
        (6, 3), // ecdsa_secp521r1_sha512
        (8, 7), // ed25519
        (2, 1), // rsa_pkcs1_sha1
    };

    private readonly int[] _cipherSuites;
    private readonly bool _ignoreCert;
    private readonly int _grease;
    private readonly string _host;
    private readonly TlsSession _legacySession; // 32-byte fake session_id per RFC 8446 Appendix D.4

    public bool NegotiatedH2 { get; private set; }

    public ChromeTlsClient(int[] cipherSuites, bool ignoreCert, string host)
        : base(TlsHelpers.SharedCrypto)
    {
        _cipherSuites = cipherSuites;
        _ignoreCert = ignoreCert;
        _grease = GreaseHelper.Pick();
        _host = host;
        var id = new byte[32];
        TlsHelpers.SharedCrypto.SecureRandom.NextBytes(id);
        _legacySession = new FakeTlsSession(id);
    }

    // Provide 32-byte random legacy_session_id so middleboxes see TLS 1.2 compat mode (Chrome behaviour).
    // IsResumable=true makes BC include the ID; ExportSessionParameters()=null prevents actual resumption.
    public override TlsSession GetSessionToResume() => _legacySession;

    private sealed class FakeTlsSession : TlsSession
    {
        private readonly byte[] _id;
        public FakeTlsSession(byte[] id) => _id = id;
        public byte[] SessionID => _id;
        public bool IsResumable => true; // lets BC include the ID in legacy_session_id
        public SessionParameters ExportSessionParameters() => null; // null → no actual resumption
        public void Invalidate() { }
    }

    protected override IList<ServerName> GetSniServerNames()
    {
        if (string.IsNullOrEmpty(_host)) return base.GetSniServerNames();
        return new List<ServerName> { new ServerName(NameType.host_name, Encoding.ASCII.GetBytes(_host)) };
    }

    public override int[] GetCipherSuites()
        => GreaseHelper.PrependCipher(_cipherSuites, _grease);

    public override TlsAuthentication GetAuthentication()
        => new BrowserTlsAuthentication(_ignoreCert, _host);

    // Chrome named groups: x25519 (29), secp256r1 (23), secp384r1 (24)
    // GREASE is prepended to the wire bytes manually in GetClientExtensions() — NOT here,
    // because BC would try to generate key material for the GREASE group and fail.
    protected override IList<int> GetSupportedGroups(IList<int> namedGroupRoles)
        => new List<int> { NamedGroup.x25519, NamedGroup.secp256r1, NamedGroup.secp384r1 };

    protected override IList<SignatureAndHashAlgorithm> GetSupportedSignatureAlgorithms()
    {
        var list = new List<SignatureAndHashAlgorithm>();
        foreach (var (h, s) in ChromeSigAlgs)
            list.Add(SignatureAndHashAlgorithm.GetInstance(h, s));
        return list;
    }

    public override IDictionary<int, byte[]> GetClientExtensions()
    {
        // Get BouncyCastle defaults. Our GetSupportedGroups() override ensures GREASE is at position
        // 0 in the supported_groups extension that BC builds from our overridden method.
        var bc = base.GetClientExtensions() ?? new Dictionary<int, byte[]>();

        // renegotiation_info (65281) — initial hello: 1 byte length prefix = 0x00 (empty)
        if (!bc.ContainsKey(ExtensionType.renegotiation_info))
            bc[ExtensionType.renegotiation_info] = new byte[] { 0x00 };

        // ALPN: h2, http/1.1
        bc[ExtensionType.application_layer_protocol_negotiation] = TlsHelpers.AlpnH2Http11;

        // status_request (OCSP) — empty request for OCSP stapling
        if (!bc.ContainsKey(ExtensionType.status_request))
            TlsExtensionsUtilities.AddStatusRequestExtension(bc,
                new CertificateStatusRequest(CertificateStatusType.ocsp, new OcspStatusRequest(null, null)));

        // signed_certificate_timestamp (18) — empty = request SCT
        bc[ExtensionType.signed_certificate_timestamp] = TlsUtilities.EmptyBytes;

        // Inject GREASE into supported_groups (prepend before x25519, secp256r1, secp384r1)
        if (bc.TryGetValue(ExtensionType.supported_groups, out var sg))
            bc[ExtensionType.supported_groups] = GreaseHelper.PrependNamedGroup(sg, _grease);

        // Inject GREASE into key_share (prepend entry with 1-byte key exchange)
        if (bc.TryGetValue(ExtensionType.key_share, out var ks))
            bc[ExtensionType.key_share] = GreaseHelper.PrependKeyShare(ks, _grease);

        // Inject GREASE into supported_versions (prepend before TLS 1.3/1.2)
        if (bc.TryGetValue(ExtensionType.supported_versions, out var sv))
            bc[ExtensionType.supported_versions] = GreaseHelper.PrependSupportedVersion(sv, _grease);

        // delegated_credentials (34): Chrome 123+ — ecdsa_p256, ecdsa_p384, rsa_pss, ecdsa_p521
        bc[34] = new byte[] { 0x00, 0x08, 0x04, 0x03, 0x05, 0x03, 0x08, 0x04, 0x06, 0x03 };
        // ALPS (17513) / compress_certificate (27) deliberately omitted:
        // ALPS: when negotiated, Google sends ALPS settings in EncryptedExtensions that the H2
        // client must process before sending frames. BouncyCastle ignores them → Google replies
        // with unexpected_message(10) on the first H2 write. Remove to skip ALPS negotiation.
        // compress_certificate: BouncyCastle cannot decompress brotli certificates → same error.

        // Chrome 131+ ClientHello extension order (matches JA4 fingerprint)
        var ext = new InsertionOrderDictionary();
        void Take(int t) { if (bc.TryGetValue(t, out var v)) ext[t] = v; }

        ext[_grease] = new byte[] { 0x00 };                                        // GREASE
        Take(ExtensionType.server_name);                                            //  0
        Take(ExtensionType.extended_master_secret);                                 // 23
        Take(ExtensionType.renegotiation_info);                                     // 65281
        Take(ExtensionType.supported_groups);                                       // 10
        Take(ExtensionType.ec_point_formats);                                       // 11
        Take(ExtensionType.session_ticket);                                         // 35
        Take(ExtensionType.application_layer_protocol_negotiation);                 // 16
        Take(ExtensionType.status_request);                                         //  5
        Take(ExtensionType.signature_algorithms);                                   // 13
        ext[ExtensionType.signed_certificate_timestamp] = TlsUtilities.EmptyBytes; // 18
        Take(34);                                                                   // delegated_credentials
        Take(ExtensionType.key_share);                                              // 51
        Take(ExtensionType.psk_key_exchange_modes);                                 // 45
        Take(ExtensionType.supported_versions);                                     // 43

        // Chrome ClientHello padding (ext type 21): pads the record to ≥512 bytes so it
        // avoids the 256–511 byte range that confuses certain middleboxes (Chrome bug #15499).
        // Include the full fixed ClientHello overhead so the estimate matches the actual wire size:
        //   4 (hdr) + 2 (version) + 32 (random) + 33 (session_id) + 2 (cipher_len) + cipherBytes + 2 (compress) + 2 (ext_len)
        int cipherBytes = GetCipherSuites().Length * 2; // GREASE already included via PrependCipher
        int estLen = 4 + 2 + 32 + 33 + 2 + cipherBytes + 2 + 2;
        foreach (var kv in ext) estLen += 4 + kv.Value.Length;
        if (estLen > 256 && estLen < 512)
        {
            int pad = Math.Max(0, 512 - estLen - 4);
            ext[21] = new byte[pad]; // type 21 = padding, all 0x00
        }

        return ext;
    }

    public override void NotifyHandshakeComplete()
    {
        base.NotifyHandshakeComplete();
        try
        {
            var sp = m_context?.SecurityParameters;
            if (sp != null && sp.IsApplicationProtocolSet)
                NegotiatedH2 = Encoding.ASCII.GetString(sp.ApplicationProtocol.GetBytes()) == "h2";
        }
        catch { }
    }

}

// ─────────────────────────────────────────────────────────────────────────────
// Firefox TLS client — no GREASE, Firefox 133+ extension order
// key_share → supported_versions → psk_modes → record_size_limit
// ─────────────────────────────────────────────────────────────────────────────
internal sealed class FirefoxTlsClient : DefaultTlsClient
{
    private readonly int[] _cipherSuites;
    private readonly bool _ignoreCert;
    private readonly string _host;
    public bool NegotiatedH2 { get; private set; }

    public FirefoxTlsClient(int[] cipherSuites, bool ignoreCert, string host)
        : base(TlsHelpers.SharedCrypto)
    {
        _cipherSuites = cipherSuites;
        _ignoreCert = ignoreCert;
        _host = host;
    }

    protected override IList<ServerName> GetSniServerNames()
    {
        if (string.IsNullOrEmpty(_host)) return base.GetSniServerNames();
        return new List<ServerName> { new ServerName(NameType.host_name, Encoding.ASCII.GetBytes(_host)) };
    }

    public override int[] GetCipherSuites() => _cipherSuites;
    public override TlsAuthentication GetAuthentication() => new BrowserTlsAuthentication(_ignoreCert, _host);

    // Firefox: x25519, secp256r1, secp384r1, secp521r1
    protected override IList<int> GetSupportedGroups(IList<int> namedGroupRoles)
        => new List<int> { NamedGroup.x25519, NamedGroup.secp256r1, NamedGroup.secp384r1, NamedGroup.secp521r1 };

    // Firefox 133+ signature_algorithms: ECDSA-first ordering
    protected override IList<SignatureAndHashAlgorithm> GetSupportedSignatureAlgorithms()
    {
        return new List<SignatureAndHashAlgorithm>
        {
            new SignatureAndHashAlgorithm(HashAlgorithm.sha256, SignatureAlgorithm.ecdsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha384, SignatureAlgorithm.ecdsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha512, SignatureAlgorithm.ecdsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.Intrinsic, SignatureAlgorithm.rsa_pss_rsae_sha256),
            new SignatureAndHashAlgorithm(HashAlgorithm.Intrinsic, SignatureAlgorithm.rsa_pss_rsae_sha384),
            new SignatureAndHashAlgorithm(HashAlgorithm.Intrinsic, SignatureAlgorithm.rsa_pss_rsae_sha512),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha256, SignatureAlgorithm.rsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha384, SignatureAlgorithm.rsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha512, SignatureAlgorithm.rsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha1,   SignatureAlgorithm.rsa),
        };
    }

    public override IDictionary<int, byte[]> GetClientExtensions()
    {
        var bc = base.GetClientExtensions() ?? new Dictionary<int, byte[]>();

        if (!bc.ContainsKey(ExtensionType.renegotiation_info))
            bc[ExtensionType.renegotiation_info] = new byte[] { 0x00 };
        bc[ExtensionType.application_layer_protocol_negotiation] = TlsHelpers.AlpnH2Http11;
        if (!bc.ContainsKey(ExtensionType.status_request))
            TlsExtensionsUtilities.AddStatusRequestExtension(bc,
                new CertificateStatusRequest(CertificateStatusType.ocsp, new OcspStatusRequest(null, null)));
        bc[ExtensionType.signed_certificate_timestamp] = TlsUtilities.EmptyBytes;
        TlsExtensionsUtilities.AddRecordSizeLimitExtension(bc, 16385);

        var ext = new InsertionOrderDictionary();
        void Take(int t) { if (bc.TryGetValue(t, out var v)) ext[t] = v; }

        Take(ExtensionType.server_name);
        Take(ExtensionType.extended_master_secret);
        Take(ExtensionType.renegotiation_info);
        Take(ExtensionType.supported_groups);
        Take(ExtensionType.ec_point_formats);
        Take(ExtensionType.session_ticket);
        Take(ExtensionType.application_layer_protocol_negotiation);
        Take(ExtensionType.status_request);
        Take(ExtensionType.signature_algorithms);
        ext[ExtensionType.signed_certificate_timestamp] = TlsUtilities.EmptyBytes;
        // Firefox 133+ order: key_share → supported_versions → psk_modes → record_size_limit
        Take(ExtensionType.key_share);
        Take(ExtensionType.supported_versions);
        Take(ExtensionType.psk_key_exchange_modes);
        Take(ExtensionType.record_size_limit);

        return ext;
    }

    public override void NotifyHandshakeComplete()
    {
        base.NotifyHandshakeComplete();
        try { var sp = m_context?.SecurityParameters; if (sp?.IsApplicationProtocolSet == true) NegotiatedH2 = Encoding.ASCII.GetString(sp.ApplicationProtocol.GetBytes()) == "h2"; } catch { }
    }

}

// ─────────────────────────────────────────────────────────────────────────────
// Safari TLS client (macOS) — encrypt_then_mac, secp521r1,
// Safari 17+ extension order: supported_versions → key_share → psk_modes
// ─────────────────────────────────────────────────────────────────────────────
internal sealed class SafariTlsClient : DefaultTlsClient
{
    private readonly int[] _cipherSuites;
    private readonly bool _ignoreCert;
    private readonly string _host;
    public bool NegotiatedH2 { get; private set; }

    public SafariTlsClient(int[] cipherSuites, bool ignoreCert, string host)
        : base(TlsHelpers.SharedCrypto)
    {
        _cipherSuites = cipherSuites;
        _ignoreCert = ignoreCert;
        _host = host;
    }

    protected override IList<ServerName> GetSniServerNames()
    {
        if (string.IsNullOrEmpty(_host)) return base.GetSniServerNames();
        return new List<ServerName> { new ServerName(NameType.host_name, Encoding.ASCII.GetBytes(_host)) };
    }

    public override int[] GetCipherSuites() => _cipherSuites;
    public override TlsAuthentication GetAuthentication() => new BrowserTlsAuthentication(_ignoreCert, _host);

    // Safari macOS 17+: x25519, secp256r1, secp384r1, secp521r1
    protected override IList<int> GetSupportedGroups(IList<int> namedGroupRoles)
        => new List<int> { NamedGroup.x25519, NamedGroup.secp256r1, NamedGroup.secp384r1, NamedGroup.secp521r1 };

    // Safari 17+ signature_algorithms (includes ecdsa_secp521r1_sha512)
    protected override IList<SignatureAndHashAlgorithm> GetSupportedSignatureAlgorithms()
    {
        return new List<SignatureAndHashAlgorithm>
        {
            new SignatureAndHashAlgorithm(HashAlgorithm.sha256, SignatureAlgorithm.ecdsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha384, SignatureAlgorithm.ecdsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha512, SignatureAlgorithm.ecdsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.Intrinsic, SignatureAlgorithm.rsa_pss_rsae_sha256),
            new SignatureAndHashAlgorithm(HashAlgorithm.Intrinsic, SignatureAlgorithm.rsa_pss_rsae_sha384),
            new SignatureAndHashAlgorithm(HashAlgorithm.Intrinsic, SignatureAlgorithm.rsa_pss_rsae_sha512),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha256, SignatureAlgorithm.rsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha384, SignatureAlgorithm.rsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha1,   SignatureAlgorithm.ecdsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha1,   SignatureAlgorithm.rsa),
        };
    }

    public override IDictionary<int, byte[]> GetClientExtensions()
    {
        var bc = base.GetClientExtensions() ?? new Dictionary<int, byte[]>();

        if (!bc.ContainsKey(ExtensionType.renegotiation_info))
            bc[ExtensionType.renegotiation_info] = new byte[] { 0x00 };
        bc[ExtensionType.application_layer_protocol_negotiation] = TlsHelpers.AlpnH2Http11;
        if (!bc.ContainsKey(ExtensionType.status_request))
            TlsExtensionsUtilities.AddStatusRequestExtension(bc,
                new CertificateStatusRequest(CertificateStatusType.ocsp, new OcspStatusRequest(null, null)));
        bc[ExtensionType.signed_certificate_timestamp] = TlsUtilities.EmptyBytes;
        if (!bc.ContainsKey(ExtensionType.encrypt_then_mac))
            TlsExtensionsUtilities.AddEncryptThenMacExtension(bc);

        var ext = new InsertionOrderDictionary();
        void Take(int t) { if (bc.TryGetValue(t, out var v)) ext[t] = v; }

        Take(ExtensionType.server_name);
        Take(ExtensionType.extended_master_secret);
        Take(ExtensionType.renegotiation_info);
        Take(ExtensionType.supported_groups);
        Take(ExtensionType.encrypt_then_mac);
        Take(ExtensionType.ec_point_formats);
        Take(ExtensionType.session_ticket);
        Take(ExtensionType.application_layer_protocol_negotiation);
        Take(ExtensionType.status_request);
        Take(ExtensionType.signature_algorithms);
        ext[ExtensionType.signed_certificate_timestamp] = TlsUtilities.EmptyBytes;
        // Safari macOS 17+ order: supported_versions → key_share → psk_modes
        Take(ExtensionType.supported_versions);
        Take(ExtensionType.key_share);
        Take(ExtensionType.psk_key_exchange_modes);

        return ext;
    }

    public override void NotifyHandshakeComplete()
    {
        base.NotifyHandshakeComplete();
        try { var sp = m_context?.SecurityParameters; if (sp?.IsApplicationProtocolSet == true) NegotiatedH2 = Encoding.ASCII.GetString(sp.ApplicationProtocol.GetBytes()) == "h2"; } catch { }
    }

}

// ─────────────────────────────────────────────────────────────────────────────
// Safari TLS client (macOS 15.x legacy) — encrypt_then_mac, NO secp521r1,
// 15.x extension order: key_share → supported_versions → psk_modes
// (Safari 15.3/15.5: secp521r1 and ecdsa_secp521r1_sha512 not yet present)
// ─────────────────────────────────────────────────────────────────────────────
internal sealed class SafariLegacyTlsClient : DefaultTlsClient
{
    private readonly int[] _cipherSuites;
    private readonly bool _ignoreCert;
    private readonly string _host;
    public bool NegotiatedH2 { get; private set; }

    public SafariLegacyTlsClient(int[] cipherSuites, bool ignoreCert, string host)
        : base(TlsHelpers.SharedCrypto)
    {
        _cipherSuites = cipherSuites;
        _ignoreCert = ignoreCert;
        _host = host;
    }

    protected override IList<ServerName> GetSniServerNames()
    {
        if (string.IsNullOrEmpty(_host)) return base.GetSniServerNames();
        return new List<ServerName> { new ServerName(NameType.host_name, Encoding.ASCII.GetBytes(_host)) };
    }

    public override int[] GetCipherSuites() => _cipherSuites;
    public override TlsAuthentication GetAuthentication() => new BrowserTlsAuthentication(_ignoreCert, _host);

    // Safari 15.x: x25519, secp256r1, secp384r1 — secp521r1 was added in Safari 17
    protected override IList<int> GetSupportedGroups(IList<int> namedGroupRoles)
        => new List<int> { NamedGroup.x25519, NamedGroup.secp256r1, NamedGroup.secp384r1 };

    // Safari 15.x signature_algorithms — NO ecdsa_secp521r1_sha512 (0x0603)
    protected override IList<SignatureAndHashAlgorithm> GetSupportedSignatureAlgorithms()
    {
        return new List<SignatureAndHashAlgorithm>
        {
            new SignatureAndHashAlgorithm(HashAlgorithm.sha256, SignatureAlgorithm.ecdsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha384, SignatureAlgorithm.ecdsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.Intrinsic, SignatureAlgorithm.rsa_pss_rsae_sha256),
            new SignatureAndHashAlgorithm(HashAlgorithm.Intrinsic, SignatureAlgorithm.rsa_pss_rsae_sha384),
            new SignatureAndHashAlgorithm(HashAlgorithm.Intrinsic, SignatureAlgorithm.rsa_pss_rsae_sha512),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha256, SignatureAlgorithm.rsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha384, SignatureAlgorithm.rsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha1,   SignatureAlgorithm.ecdsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha1,   SignatureAlgorithm.rsa),
        };
    }

    public override IDictionary<int, byte[]> GetClientExtensions()
    {
        var bc = base.GetClientExtensions() ?? new Dictionary<int, byte[]>();

        if (!bc.ContainsKey(ExtensionType.renegotiation_info))
            bc[ExtensionType.renegotiation_info] = new byte[] { 0x00 };
        bc[ExtensionType.application_layer_protocol_negotiation] = TlsHelpers.AlpnH2Http11;
        if (!bc.ContainsKey(ExtensionType.status_request))
            TlsExtensionsUtilities.AddStatusRequestExtension(bc,
                new CertificateStatusRequest(CertificateStatusType.ocsp, new OcspStatusRequest(null, null)));
        bc[ExtensionType.signed_certificate_timestamp] = TlsUtilities.EmptyBytes;
        if (!bc.ContainsKey(ExtensionType.encrypt_then_mac))
            TlsExtensionsUtilities.AddEncryptThenMacExtension(bc);

        var ext = new InsertionOrderDictionary();
        void Take(int t) { if (bc.TryGetValue(t, out var v)) ext[t] = v; }

        Take(ExtensionType.server_name);
        Take(ExtensionType.extended_master_secret);
        Take(ExtensionType.renegotiation_info);
        Take(ExtensionType.supported_groups);
        Take(ExtensionType.encrypt_then_mac);
        Take(ExtensionType.ec_point_formats);
        Take(ExtensionType.session_ticket);
        Take(ExtensionType.application_layer_protocol_negotiation);
        Take(ExtensionType.status_request);
        Take(ExtensionType.signature_algorithms);
        ext[ExtensionType.signed_certificate_timestamp] = TlsUtilities.EmptyBytes;
        // Safari 15.x order: key_share → supported_versions → psk_modes
        Take(ExtensionType.key_share);
        Take(ExtensionType.supported_versions);
        Take(ExtensionType.psk_key_exchange_modes);

        return ext;
    }

    public override void NotifyHandshakeComplete()
    {
        base.NotifyHandshakeComplete();
        try { var sp = m_context?.SecurityParameters; if (sp?.IsApplicationProtocolSet == true) NegotiatedH2 = Encoding.ASCII.GetString(sp.ApplicationProtocol.GetBytes()) == "h2"; } catch { }
    }

}

// ─────────────────────────────────────────────────────────────────────────────
// Safari TLS client (iOS) — no encrypt_then_mac
// iOS 17+ extension order: key_share → supported_versions → psk_modes
// ─────────────────────────────────────────────────────────────────────────────
internal sealed class SafariiOSTlsClient : DefaultTlsClient
{
    private readonly int[] _cipherSuites;
    private readonly bool _ignoreCert;
    private readonly string _host;
    public bool NegotiatedH2 { get; private set; }

    public SafariiOSTlsClient(int[] cipherSuites, bool ignoreCert, string host)
        : base(TlsHelpers.SharedCrypto)
    {
        _cipherSuites = cipherSuites;
        _ignoreCert = ignoreCert;
        _host = host;
    }

    protected override IList<ServerName> GetSniServerNames()
    {
        if (string.IsNullOrEmpty(_host)) return base.GetSniServerNames();
        return new List<ServerName> { new ServerName(NameType.host_name, Encoding.ASCII.GetBytes(_host)) };
    }

    public override int[] GetCipherSuites() => _cipherSuites;
    public override TlsAuthentication GetAuthentication() => new BrowserTlsAuthentication(_ignoreCert, _host);

    // Safari iOS 17+: x25519, secp256r1, secp384r1, secp521r1
    protected override IList<int> GetSupportedGroups(IList<int> namedGroupRoles)
        => new List<int> { NamedGroup.x25519, NamedGroup.secp256r1, NamedGroup.secp384r1, NamedGroup.secp521r1 };

    // Safari iOS 17+ signature_algorithms (same as macOS Safari)
    protected override IList<SignatureAndHashAlgorithm> GetSupportedSignatureAlgorithms()
    {
        return new List<SignatureAndHashAlgorithm>
        {
            new SignatureAndHashAlgorithm(HashAlgorithm.sha256, SignatureAlgorithm.ecdsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha384, SignatureAlgorithm.ecdsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha512, SignatureAlgorithm.ecdsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.Intrinsic, SignatureAlgorithm.rsa_pss_rsae_sha256),
            new SignatureAndHashAlgorithm(HashAlgorithm.Intrinsic, SignatureAlgorithm.rsa_pss_rsae_sha384),
            new SignatureAndHashAlgorithm(HashAlgorithm.Intrinsic, SignatureAlgorithm.rsa_pss_rsae_sha512),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha256, SignatureAlgorithm.rsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha384, SignatureAlgorithm.rsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha1,   SignatureAlgorithm.ecdsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha1,   SignatureAlgorithm.rsa),
        };
    }

    public override IDictionary<int, byte[]> GetClientExtensions()
    {
        var bc = base.GetClientExtensions() ?? new Dictionary<int, byte[]>();

        if (!bc.ContainsKey(ExtensionType.renegotiation_info))
            bc[ExtensionType.renegotiation_info] = new byte[] { 0x00 };
        bc[ExtensionType.application_layer_protocol_negotiation] = TlsHelpers.AlpnH2Http11;
        if (!bc.ContainsKey(ExtensionType.status_request))
            TlsExtensionsUtilities.AddStatusRequestExtension(bc,
                new CertificateStatusRequest(CertificateStatusType.ocsp, new OcspStatusRequest(null, null)));
        bc[ExtensionType.signed_certificate_timestamp] = TlsUtilities.EmptyBytes;
        // iOS does NOT include encrypt_then_mac
        // compress_certificate (27) deliberately omitted: BouncyCastle cannot decompress
        // brotli-compressed certificates → server sends CompressedCertificate → unexpected_message(10).

        var ext = new InsertionOrderDictionary();
        void Take(int t) { if (bc.TryGetValue(t, out var v)) ext[t] = v; }

        Take(ExtensionType.server_name);
        Take(ExtensionType.extended_master_secret);
        Take(ExtensionType.renegotiation_info);
        Take(ExtensionType.supported_groups);
        Take(ExtensionType.ec_point_formats);
        Take(ExtensionType.session_ticket);
        Take(ExtensionType.application_layer_protocol_negotiation);
        Take(ExtensionType.status_request);
        Take(ExtensionType.signature_algorithms);
        ext[ExtensionType.signed_certificate_timestamp] = TlsUtilities.EmptyBytes;
        // Safari iOS 17+ order: key_share → supported_versions → psk_modes
        Take(ExtensionType.key_share);
        Take(ExtensionType.supported_versions);
        Take(ExtensionType.psk_key_exchange_modes);

        return ext;
    }

    public override void NotifyHandshakeComplete()
    {
        base.NotifyHandshakeComplete();
        try { var sp = m_context?.SecurityParameters; if (sp?.IsApplicationProtocolSet == true) NegotiatedH2 = Encoding.ASCII.GetString(sp.ApplicationProtocol.GetBytes()) == "h2"; } catch { }
    }

}

internal enum H2Mode { Chrome, Firefox, Safari, OkHttp }

// ─────────────────────────────────────────────────────────────────────────────
// OkHttp 4.x TLS client (Android Conscrypt / BoringSSL)
// No GREASE, no SCT, no renegotiation_info, no encrypt_then_mac, no padding
// Extension order: SNI, extended_master_secret, session_ticket,
//   supported_groups, ec_point_formats, sig_algs, ALPN,
//   key_share, supported_versions, psk_modes
// ─────────────────────────────────────────────────────────────────────────────
internal sealed class OkhttpTlsClient : DefaultTlsClient
{
    private readonly int[] _cipherSuites;
    private readonly bool _ignoreCert;
    private readonly string _host;
    public bool NegotiatedH2 { get; private set; }

    public OkhttpTlsClient(int[] cipherSuites, bool ignoreCert, string host)
        : base(TlsHelpers.SharedCrypto)
    {
        _cipherSuites = cipherSuites;
        _ignoreCert = ignoreCert;
        _host = host;
    }

    protected override IList<ServerName> GetSniServerNames()
    {
        if (string.IsNullOrEmpty(_host)) return base.GetSniServerNames();
        return new List<ServerName> { new ServerName(NameType.host_name, Encoding.ASCII.GetBytes(_host)) };
    }

    public override int[] GetCipherSuites() => _cipherSuites;
    public override TlsAuthentication GetAuthentication() => new BrowserTlsAuthentication(_ignoreCert, _host);

    // Android 10+ Conscrypt: x25519, secp256r1, secp384r1, secp521r1
    protected override IList<int> GetSupportedGroups(IList<int> namedGroupRoles)
        => new List<int> { NamedGroup.x25519, NamedGroup.secp256r1, NamedGroup.secp384r1, NamedGroup.secp521r1 };

    // Android Conscrypt signature algorithms (BoringSSL order)
    protected override IList<SignatureAndHashAlgorithm> GetSupportedSignatureAlgorithms()
    {
        return new List<SignatureAndHashAlgorithm>
        {
            new SignatureAndHashAlgorithm(HashAlgorithm.sha256, SignatureAlgorithm.ecdsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.Intrinsic, SignatureAlgorithm.rsa_pss_rsae_sha256),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha256, SignatureAlgorithm.rsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha384, SignatureAlgorithm.ecdsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.Intrinsic, SignatureAlgorithm.rsa_pss_rsae_sha384),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha384, SignatureAlgorithm.rsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.Intrinsic, SignatureAlgorithm.rsa_pss_rsae_sha512),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha512, SignatureAlgorithm.rsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha1,   SignatureAlgorithm.rsa),
        };
    }

    public override IDictionary<int, byte[]> GetClientExtensions()
    {
        var bc = base.GetClientExtensions() ?? new Dictionary<int, byte[]>();

        // OkHttp/Conscrypt does NOT send renegotiation_info in Android 10+
        bc.Remove(ExtensionType.renegotiation_info);

        bc[ExtensionType.application_layer_protocol_negotiation] = TlsHelpers.AlpnH2Http11;
        // No signed_certificate_timestamp, no encrypt_then_mac

        var ext = new InsertionOrderDictionary();
        void Take(int t) { if (bc.TryGetValue(t, out var v)) ext[t] = v; }

        Take(ExtensionType.server_name);
        Take(ExtensionType.extended_master_secret);
        Take(ExtensionType.session_ticket);
        Take(ExtensionType.supported_groups);
        Take(ExtensionType.ec_point_formats);
        Take(ExtensionType.signature_algorithms);
        Take(ExtensionType.application_layer_protocol_negotiation);
        Take(ExtensionType.key_share);
        Take(ExtensionType.supported_versions);
        Take(ExtensionType.psk_key_exchange_modes);

        return ext;
    }

    public override void NotifyHandshakeComplete()
    {
        base.NotifyHandshakeComplete();
        try { var sp = m_context?.SecurityParameters; if (sp?.IsApplicationProtocolSet == true) NegotiatedH2 = Encoding.ASCII.GetString(sp.ApplicationProtocol.GetBytes()) == "h2"; } catch { }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Tor Browser 14.5 TLS client (Firefox ESR 128 base, privacy-hardened)
// Key differences from regular Firefox: no session_ticket (disabled for
// anti-tracking), no signed_certificate_timestamp, record_size_limit included
// ─────────────────────────────────────────────────────────────────────────────
internal sealed class TorTlsClient : DefaultTlsClient
{
    private readonly int[] _cipherSuites;
    private readonly bool _ignoreCert;
    private readonly string _host;
    public bool NegotiatedH2 { get; private set; }

    public TorTlsClient(int[] cipherSuites, bool ignoreCert, string host)
        : base(TlsHelpers.SharedCrypto)
    {
        _cipherSuites = cipherSuites;
        _ignoreCert = ignoreCert;
        _host = host;
    }

    protected override IList<ServerName> GetSniServerNames()
    {
        if (string.IsNullOrEmpty(_host)) return base.GetSniServerNames();
        return new List<ServerName> { new ServerName(NameType.host_name, Encoding.ASCII.GetBytes(_host)) };
    }

    public override int[] GetCipherSuites() => _cipherSuites;
    public override TlsAuthentication GetAuthentication() => new BrowserTlsAuthentication(_ignoreCert, _host);

    // Tor 14.5: x25519, secp256r1, secp384r1, secp521r1 (same as Firefox)
    protected override IList<int> GetSupportedGroups(IList<int> namedGroupRoles)
        => new List<int> { NamedGroup.x25519, NamedGroup.secp256r1, NamedGroup.secp384r1, NamedGroup.secp521r1 };

    // Tor uses Firefox ESR 128 signature algorithms
    protected override IList<SignatureAndHashAlgorithm> GetSupportedSignatureAlgorithms()
    {
        return new List<SignatureAndHashAlgorithm>
        {
            new SignatureAndHashAlgorithm(HashAlgorithm.sha256, SignatureAlgorithm.ecdsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha384, SignatureAlgorithm.ecdsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha512, SignatureAlgorithm.ecdsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.Intrinsic, SignatureAlgorithm.rsa_pss_rsae_sha256),
            new SignatureAndHashAlgorithm(HashAlgorithm.Intrinsic, SignatureAlgorithm.rsa_pss_rsae_sha384),
            new SignatureAndHashAlgorithm(HashAlgorithm.Intrinsic, SignatureAlgorithm.rsa_pss_rsae_sha512),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha256, SignatureAlgorithm.rsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha384, SignatureAlgorithm.rsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha512, SignatureAlgorithm.rsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha1,   SignatureAlgorithm.ecdsa),
            new SignatureAndHashAlgorithm(HashAlgorithm.sha1,   SignatureAlgorithm.rsa),
        };
    }

    public override IDictionary<int, byte[]> GetClientExtensions()
    {
        var bc = base.GetClientExtensions() ?? new Dictionary<int, byte[]>();

        if (!bc.ContainsKey(ExtensionType.renegotiation_info))
            bc[ExtensionType.renegotiation_info] = new byte[] { 0x00 };
        bc[ExtensionType.application_layer_protocol_negotiation] = TlsHelpers.AlpnH2Http11;
        if (!bc.ContainsKey(ExtensionType.status_request))
            TlsExtensionsUtilities.AddStatusRequestExtension(bc,
                new CertificateStatusRequest(CertificateStatusType.ocsp, new OcspStatusRequest(null, null)));
        // record_size_limit = 16385 (Firefox/Tor extension)
        bc[28] = new byte[] { 0x40, 0x01 };

        // Tor deliberately omits session_ticket (prevents tracking)
        bc.Remove(ExtensionType.session_ticket);
        // Tor omits signed_certificate_timestamp (privacy)

        var ext = new InsertionOrderDictionary();
        void Take(int t) { if (bc.TryGetValue(t, out var v)) ext[t] = v; }

        Take(ExtensionType.server_name);
        Take(ExtensionType.extended_master_secret);
        Take(ExtensionType.renegotiation_info);
        Take(ExtensionType.supported_groups);
        Take(ExtensionType.ec_point_formats);
        Take(ExtensionType.application_layer_protocol_negotiation);
        Take(ExtensionType.status_request);
        Take(ExtensionType.signature_algorithms);
        Take(ExtensionType.key_share);
        Take(ExtensionType.supported_versions);
        Take(ExtensionType.psk_key_exchange_modes);
        ext[28] = new byte[] { 0x40, 0x01 }; // record_size_limit at end

        return ext;
    }

    public override void NotifyHandshakeComplete()
    {
        base.NotifyHandshakeComplete();
        try { var sp = m_context?.SecurityParameters; if (sp?.IsApplicationProtocolSet == true) NegotiatedH2 = Encoding.ASCII.GetString(sp.ApplicationProtocol.GetBytes()) == "h2"; } catch { }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Minimal HTTP/2 client — per-browser SETTINGS, core frame handling,
// HPACK literal encoding for requests and basic decoding for responses
// ─────────────────────────────────────────────────────────────────────────────
internal sealed class Http2Client
{
    private static readonly byte[] H2Preface = Encoding.ASCII.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n");

    // RFC 7540 §8.1.2.2 and §8.1.2.3: these headers are connection-specific or
    // covered by H2 pseudo-headers and must NOT appear in HTTP/2 HPACK frames.
    private static readonly HashSet<string> _h2ForbiddenHeaders = new(StringComparer.OrdinalIgnoreCase)
        { "host", "connection", "keep-alive", "transfer-encoding", "upgrade", "proxy-connection" };

    // Chrome SETTINGS: HEADER_TABLE_SIZE=65536, ENABLE_PUSH=0, INITIAL_WINDOW_SIZE=6291456, MAX_HEADER_LIST_SIZE=262144
    private static readonly byte[] ChromeH2Settings = BuildSettingsPayload(
        (0x1, 65536), (0x2, 0), (0x4, 6291456), (0x6, 262144));

    // Firefox SETTINGS: HEADER_TABLE_SIZE=65536, INITIAL_WINDOW_SIZE=131072, MAX_FRAME_SIZE=16384
    private static readonly byte[] FirefoxH2Settings = BuildSettingsPayload(
        (0x1, 65536), (0x4, 131072), (0x5, 16384));

    // Safari SETTINGS: only INITIAL_WINDOW_SIZE=2097152 (per Safari captures)
    private static readonly byte[] SafariH2Settings = BuildSettingsPayload(
        (0x4, 2097152));

    // OkHttp H2 SETTINGS: HEADER_TABLE_SIZE=0, ENABLE_PUSH=0, INITIAL_WINDOW_SIZE=16777216
    private static readonly byte[] OkhttpH2Settings = BuildSettingsPayload(
        (0x1, 0), (0x2, 0), (0x4, 16777216));

    private readonly Stream _stream;
    private readonly H2Mode _mode;
    private int _serverConnWindow      = 65535; // RFC 7540 §6.9.2 default connection send window
    private int _serverStreamWindow    = 65535; // per-stream send window for current stream
    private int _serverInitStreamWindow = 65535; // server's INITIAL_WINDOW_SIZE — reset point per new stream
    private int _serverMaxFrameSize    = 16384;  // RFC 7540 §6.5.2: default, updated via SETTINGS ID=5
    private int _nextStreamId = 1;              // RFC 7540 §5.1.1: client uses odd IDs, starting at 1
    private bool _goAway = false;            // set on GOAWAY — connection must not be reused
    private readonly HpackDecoder _hpackDecoder = new HpackDecoder(); // persists dynamic table
    private readonly HpackEncoderState _hpackEnc = new();            // stateful encoder — reuses dynamic table across requests

    public bool IsUsable => !_goAway && _nextStreamId > 0; // _nextStreamId < 0 means it overflowed RFC 7540 §5.1.1
    public void Dispose() { try { _stream.Dispose(); } catch { } }

    public Http2Client(Stream stream, H2Mode mode) { _stream = stream; _mode = mode; }

    private void ApplyServerSettings(byte[] payload)
    {
        for (int pi = 0; pi + 6 <= payload.Length; pi += 6)
        {
            int id  = (payload[pi] << 8) | payload[pi + 1];
            int val = (payload[pi + 2] << 24) | (payload[pi + 3] << 16) | (payload[pi + 4] << 8) | payload[pi + 5];
            if (id == 1) _hpackEnc.SetMaxSize(val); // RFC 7541 §4.2: notify encoder of new table size limit
            if (id == 4)
            {
                // RFC 7540 §6.9.2: adjust current stream window by the delta, don't replace it.
                // Window CAN go negative (sender must block until WINDOW_UPDATE brings it positive again).
                int delta = val - _serverInitStreamWindow;
                _serverStreamWindow = (int)Math.Clamp((long)_serverStreamWindow + delta, int.MinValue, int.MaxValue);
                _serverInitStreamWindow = val;
            }
            if (id == 5 && val >= 16384 && val <= 16777215) _serverMaxFrameSize = val; // RFC 7540 §6.5.2
        }
    }

    public async Task HandshakeAsync(CancellationToken ct)
    {
        byte[] sp = _mode == H2Mode.Chrome  ? ChromeH2Settings
                  : _mode == H2Mode.Firefox ? FirefoxH2Settings
                  : _mode == H2Mode.OkHttp  ? OkhttpH2Settings
                  : SafariH2Settings;
        uint winInc = _mode == H2Mode.Chrome  ? 15663105u
                    : _mode == H2Mode.Firefox ? 12517377u
                    : _mode == H2Mode.OkHttp  ? 16711681u  // 0xFF0001 — Android default
                    : 2147418112u; // Safari: 0x7FFF0000 = max connection window

        // Chrome sends preface + SETTINGS + WINDOW_UPDATE in one write() → one TLS record.
        // Splitting into 3 separate writes produces 3 TLS records, which fingerprinters detect.
        int settingsPayloadLen = sp.Length;
        var preface = new byte[H2Preface.Length + 9 + settingsPayloadLen + 13];
        int pos = 0;
        H2Preface.CopyTo(preface, pos); pos += H2Preface.Length;
        // SETTINGS frame header (len | type=0x4 | flags=0x0 | sid=0)
        preface[pos++] = (byte)(settingsPayloadLen >> 16);
        preface[pos++] = (byte)(settingsPayloadLen >> 8);
        preface[pos++] = (byte)(settingsPayloadLen & 0xFF);
        preface[pos++] = 0x4; preface[pos++] = 0x0;
        preface[pos++] = 0; preface[pos++] = 0; preface[pos++] = 0; preface[pos++] = 0;
        sp.CopyTo(preface, pos); pos += settingsPayloadLen;
        // WINDOW_UPDATE frame (len=4 | type=0x8 | flags=0x0 | sid=0 | increment)
        preface[pos++] = 0; preface[pos++] = 0; preface[pos++] = 4;
        preface[pos++] = 0x8; preface[pos++] = 0x0;
        preface[pos++] = 0; preface[pos++] = 0; preface[pos++] = 0; preface[pos++] = 0;
        preface[pos++] = (byte)(winInc >> 24); preface[pos++] = (byte)(winInc >> 16);
        preface[pos++] = (byte)(winInc >> 8);  preface[pos]   = (byte)winInc;
        await _stream.WriteAsync(preface.AsMemory(), ct);

        bool gotSettings = false;
        for (int i = 0; i < 20; i++)
        {
            var (type, flags, _, payload) = await ReadFrameAsync(ct);
            if (type == 0x4 && (flags & 0x1) == 0)
            {
                ApplyServerSettings(payload); // RFC 7540 §6.5: apply before sending ACK
                await WriteFrameAsync(0x4, 0x1, 0, Array.Empty<byte>(), ct);
                gotSettings = true;
                break;
            }
            if (type == 0x6 && (flags & 0x1) == 0) await WriteFrameAsync(0x6, 0x1, 0, payload, ct);
            if (type == 0x7) { _goAway = true; throw new HttpRequestException("Server sent GOAWAY during H2 handshake"); }
        }
        if (!gotSettings)
            throw new HttpRequestException("HTTP/2 handshake: server did not send SETTINGS within expected window");
    }

    public async Task<HttpResponseMessage> SendRequestAsync(
        HttpRequestMessage request, byte[] body, CancellationToken ct)
    {
        var uri = request.RequestUri;
        string path = uri.PathAndQuery.Length > 0 ? uri.PathAndQuery : "/";
        string authority = uri.Host + (uri.IsDefaultPort ? "" : ":" + uri.Port);

        // Pseudo-header order is part of the Akamai H2 fingerprint (same signal Cloudflare reads).
        // Chrome/Safari: :method :authority :scheme :path
        // Firefox:       :method :path :authority :scheme
        // OkHttp:        :method :path :scheme :authority  (Android Java HTTP/2 client)
        var hdrs = _mode == H2Mode.Firefox
            ? new List<(string, string)>
            {
                (":method",    request.Method.Method),
                (":path",      path),
                (":authority", authority),
                (":scheme",    uri.Scheme),
            }
            : _mode == H2Mode.OkHttp
            ? new List<(string, string)>
            {
                (":method",    request.Method.Method),
                (":path",      path),
                (":scheme",    uri.Scheme),
                (":authority", authority),
            }
            : new List<(string, string)>
            {
                (":method",    request.Method.Method),
                (":authority", authority),
                (":scheme",    uri.Scheme),
                (":path",      path),
            };
        foreach (var h in request.Headers)
            if (!_h2ForbiddenHeaders.Contains(h.Key))
            {
                string joined = h.Key.Equals("Cookie", StringComparison.OrdinalIgnoreCase)
                    ? string.Join("; ", h.Value) : string.Join(", ", h.Value);
                hdrs.Add((h.Key.ToLowerInvariant(), joined));
            }
        if (request.Content != null)
            foreach (var h in request.Content.Headers)
                if (!_h2ForbiddenHeaders.Contains(h.Key))
                {
                    string joined = h.Key.Equals("Cookie", StringComparison.OrdinalIgnoreCase)
                        ? string.Join("; ", h.Value) : string.Join(", ", h.Value);
                    hdrs.Add((h.Key.ToLowerInvariant(), joined));
                }
        if (body != null && !hdrs.Any(h => h.Item1 == "content-length"))
            hdrs.Add(("content-length", body.Length.ToString()));

        int streamId = _nextStreamId;
        _nextStreamId += 2;
        _serverStreamWindow = _serverInitStreamWindow; // reset to server's advertised INITIAL_WINDOW_SIZE

        byte[] hdrBlock = _hpackEnc.Encode(hdrs);
        bool es = body == null || body.Length == 0;
        await WriteFrameAsync(0x1, (byte)(0x4 | (es ? 0x1 : 0x0)), streamId, hdrBlock, ct);
        if (!es)
        {
            // RFC 7540 §6.9: send body in flow-control windows, waiting for WINDOW_UPDATE between chunks
            int remaining = body.Length;
            int offset = 0;
            while (remaining > 0)
            {
                // Block until both windows have space
                while (_serverConnWindow <= 0 || _serverStreamWindow <= 0)
                {
                    var (wtype, wflags, wsid, wpayload) = await ReadFrameAsync(ct);
                    if (wtype == 0x8 && wpayload.Length >= 4)
                    {
                        int inc = (int)(((wpayload[0] & 0x7Fu) << 24) | ((uint)wpayload[1] << 16) | ((uint)wpayload[2] << 8) | wpayload[3]);
                        if (wsid == 0)          _serverConnWindow   = (int)Math.Min((long)_serverConnWindow   + inc, int.MaxValue);
                        else if (wsid==streamId) _serverStreamWindow = (int)Math.Min((long)_serverStreamWindow + inc, int.MaxValue);
                    }
                    else if (wtype == 0x1 && wpayload.Length > 0)
                    {
                        // 1xx informational HEADERS (100 Continue, 103 Early Hints) can arrive during a POST body send.
                        // Must decode the HPACK block to keep dynamic table in sync — skipping it desynchronises
                        // the table and causes index-out-of-range errors when decoding the real 200 response.
                        int hOff = 0;
                        if ((wflags & 0x8) != 0) hOff = 1;       // PADDED: skip pad_length byte
                        if ((wflags & 0x20) != 0) hOff += 5;     // PRIORITY: skip 5-byte priority block
                        int hEnd = (wflags & 0x8) != 0 ? wpayload.Length - wpayload[0] : wpayload.Length;
                        if (hOff < hEnd) _hpackDecoder.Decode(wpayload[hOff..hEnd]);
                    }
                    else if (wtype == 0x4 && (wflags & 0x1) == 0) { ApplyServerSettings(wpayload); await WriteFrameAsync(0x4, 0x1, 0, Array.Empty<byte>(), ct); }
                    else if (wtype == 0x6 && (wflags & 0x1) == 0) await WriteFrameAsync(0x6, 0x1, 0, wpayload, ct);
                    else if (wtype == 0x7) { _goAway = true; throw new HttpRequestException("HTTP/2 GOAWAY during body send"); }
                    else if (wtype == 0x3 && wsid == streamId) throw new HttpRequestException("HTTP/2 RST_STREAM during body send");
                }
                int chunkSize = Math.Min(remaining, Math.Min(_serverConnWindow, _serverStreamWindow));
                bool lastChunk = chunkSize >= remaining;
                _serverConnWindow   -= chunkSize;
                _serverStreamWindow -= chunkSize;
                // WriteFrameAsync further splits by _serverMaxFrameSize (frames within the chunk)
                await WriteFrameAsync(0x0, lastChunk ? (byte)0x1 : (byte)0x0, streamId, body, offset, chunkSize, ct);
                offset    += chunkSize;
                remaining -= chunkSize;
            }
        }

        return await ReadResponseAsync(request, streamId, ct);
    }

    private async Task<HttpResponseMessage> ReadResponseAsync(HttpRequestMessage orig, int streamId, CancellationToken ct)
    {
        var hpack = _hpackDecoder;
        var respHdrs = new List<(string Name, string Value)>();
        using var bodyMs = new MemoryStream();
        bool done = false;

        while (!done)
        {
            var (type, flags, sid, payload) = await ReadFrameAsync(ct);
            switch (type)
            {
                case 0x0: // DATA
                {
                    // RFC 7540 §6.9.1: ALL DATA frames consume the connection window, regardless of stream.
                    // Credit the connection window for frames from other streams (e.g. server push we rejected).
                    if (sid != streamId && payload.Length > 0)
                    {
                        // RFC 7540 §6.9.1: padding bytes also consume the flow-control window — use full payload.Length.
                        int connBytes = payload.Length;
                        if (connBytes > 0)
                        {
                            var wu = new byte[13]; // single WINDOW_UPDATE frame for connection (sid=0)
                            wu[2] = 4; wu[3] = 0x8; // len=4, type=WINDOW_UPDATE, flags=0, sid=0
                            wu[9] = (byte)(connBytes >> 24); wu[10] = (byte)(connBytes >> 16);
                            wu[11] = (byte)(connBytes >> 8); wu[12] = (byte)connBytes;
                            await _stream.WriteAsync(wu.AsMemory(), ct);
                        }
                    }
                    if (sid == streamId)
                    {
                        // Strip padding if PADDED flag (0x8) is set — RFC 7540 §6.1
                        byte[] dataPayload = payload;
                        if (payload.Length > 0 && (flags & 0x8) != 0)
                        {
                            int padLen = dataPayload[0];
                            int dataLen = dataPayload.Length - 1 - padLen;
                            dataPayload = dataLen > 0 ? dataPayload[1..(1 + dataLen)] : Array.Empty<byte>();
                        }
                        if (dataPayload.Length > 0)
                        {
                            bodyMs.Write(dataPayload, 0, dataPayload.Length);
                            // RFC 7540 §6.9.1: WINDOW_UPDATE increment must equal full DATA payload consumed
                            // (including padding), not just application data. Use pre-strip payload.Length.
                            int winInc = payload.Length;
                            var w = new byte[26]; // 2 × (9-byte header + 4-byte payload)
                            // Frame 1: connection WINDOW_UPDATE (sid=0)
                            w[2] = 4; w[3] = 0x8; // len=4, type=0x8, flags=0, sid=0
                            w[9]  = (byte)(winInc >> 24); w[10] = (byte)(winInc >> 16);
                            w[11] = (byte)(winInc >> 8);  w[12] = (byte)winInc;
                            // Frame 2: stream WINDOW_UPDATE
                            int s2 = streamId & 0x7FFFFFFF;
                            w[15] = 4; w[16] = 0x8; // len=4, type=0x8, flags=0
                            w[18] = (byte)(s2 >> 24); w[19] = (byte)(s2 >> 16);
                            w[20] = (byte)(s2 >> 8);  w[21] = (byte)s2;
                            w[22] = (byte)(winInc >> 24); w[23] = (byte)(winInc >> 16);
                            w[24] = (byte)(winInc >> 8);  w[25] = (byte)winInc;
                            await _stream.WriteAsync(w.AsMemory(), ct);
                        }
                        if ((flags & 0x1) != 0) done = true;
                    }
                    break;
                }

                case 0x1: // HEADERS
                {
                    // Strip PADDED prefix — RFC 7540 §6.2
                    byte[] hdrPayload = payload;
                    int hdrOffset = 0;
                    int padLen2 = 0;
                    if ((flags & 0x8) != 0 && hdrPayload.Length > 0)
                    {
                        padLen2 = hdrPayload[0];
                        hdrOffset = 1;
                    }
                    // Strip PRIORITY block (5 bytes) — RFC 7540 §6.2
                    if ((flags & 0x20) != 0) hdrOffset += 5;

                    int hdrEnd = hdrPayload.Length - padLen2;
                    if (hdrEnd < 0 || hdrOffset > hdrEnd)
                        throw new HttpRequestException("H2 HEADERS frame: invalid PADDED/PRIORITY combination");
                    byte[] hpackData = (hdrOffset > 0 || padLen2 > 0)
                        ? hdrPayload[hdrOffset..hdrEnd]
                        : hdrPayload;

                    // Accumulate CONTINUATION frames if END_HEADERS (0x4) is not set — RFC 7540 §6.10
                    // MemoryStream avoids O(N²) copies when there are multiple CONTINUATION frames.
                    bool endHdrs = (flags & 0x4) != 0;
                    if (!endHdrs)
                    {
                        using var acc = new MemoryStream(hpackData.Length * 2);
                        acc.Write(hpackData, 0, hpackData.Length);
                        while (!endHdrs)
                        {
                            var (ctype, cflags, _, cpayload) = await ReadFrameAsync(ct);
                            if (ctype != 0x9) throw new HttpRequestException($"H2 protocol error: expected CONTINUATION (9), got frame type {ctype}");
                            endHdrs = (cflags & 0x4) != 0;
                            acc.Write(cpayload, 0, cpayload.Length);
                        }
                        hpackData = acc.ToArray();
                    }

                    // Always decode to keep HPACK dynamic table in sync, even for other streams
                    var decoded = hpack.Decode(hpackData);
                    if (sid == streamId)
                    {
                        // Discard 1xx informational frames (100 Continue, 103 Early Hints…)
                        var statusVal = decoded.FirstOrDefault(h => h.Name == ":status").Value;
                        if (statusVal != null && int.TryParse(statusVal, out int isc) && isc < 200)
                            break;
                        respHdrs.AddRange(decoded);
                        if ((flags & 0x1) != 0) done = true;
                    }
                    break;
                }

                case 0x4: // SETTINGS
                    if ((flags & 0x1) == 0) { ApplyServerSettings(payload); await WriteFrameAsync(0x4, 0x1, 0, Array.Empty<byte>(), ct); }
                    break;
                case 0x6: // PING
                    if ((flags & 0x1) == 0) await WriteFrameAsync(0x6, 0x1, 0, payload, ct); break;
                case 0x8: // WINDOW_UPDATE — update our available send quota
                    if (payload.Length >= 4)
                    {
                        int inc = (int)(((payload[0] & 0x7Fu) << 24) | ((uint)payload[1] << 16) | ((uint)payload[2] << 8) | payload[3]);
                        if (sid == 0)           _serverConnWindow   = (int)Math.Min((long)_serverConnWindow   + inc, int.MaxValue);
                        else if (sid==streamId) _serverStreamWindow = (int)Math.Min((long)_serverStreamWindow + inc, int.MaxValue);
                        // else: WINDOW_UPDATE for a stream we don't own — ignore
                    }
                    break;
                case 0x5: // PUSH_PROMISE — reject with RST_STREAM REFUSED_STREAM (RFC 7540 §8.2)
                    if (payload.Length >= 4)
                    {
                        int ppOff = (flags & 0x8) != 0 ? 1 : 0; // PADDED: payload[0] is pad_length, not promisedId
                        if (ppOff + 4 <= payload.Length)
                        {
                            int promisedId = ((payload[ppOff] & 0x7F) << 24) | (payload[ppOff + 1] << 16)
                                           | (payload[ppOff + 2] << 8)        |  payload[ppOff + 3];
                            // Decode HPACK block to keep dynamic table in sync (RFC 7541 §2.2).
                            // Must exclude trailing padding — RFC 7540 §6.6: pad_length is in payload[0] when PADDED.
                            int hblockStart = ppOff + 4;
                            int hblockEnd   = (flags & 0x8) != 0 ? payload.Length - payload[0] : payload.Length;
                            if (hblockStart < hblockEnd)
                                hpack.Decode(payload[hblockStart..hblockEnd]);
                            // RFC 7540 §6.6: if END_HEADERS (0x4) not set, CONTINUATION frames follow.
                            // Must drain them to keep HPACK dynamic table in sync with the server.
                            bool endPushHdrs = (flags & 0x4) != 0;
                            while (!endPushHdrs)
                            {
                                var (ctype2, cflags2, _, cpayload2) = await ReadFrameAsync(ct);
                                if (ctype2 != 0x9) throw new HttpRequestException($"H2 protocol error: expected CONTINUATION (9) after PUSH_PROMISE, got {ctype2}");
                                if (cpayload2.Length > 0) hpack.Decode(cpayload2);
                                endPushHdrs = (cflags2 & 0x4) != 0;
                            }
                            await WriteFrameAsync(0x3, 0x0, promisedId, BigEndian4(7), ct); // 7 = REFUSED_STREAM
                        }
                    }
                    break;
                case 0x3:
                    if (sid == streamId) throw new HttpRequestException("HTTP/2 RST_STREAM");
                    break; // RST for another stream (e.g. rejected push) — ignore
                case 0x7: _goAway = true; throw new HttpRequestException("HTTP/2 GOAWAY");
            }
        }

        int sc = int.TryParse(respHdrs.FirstOrDefault(h => h.Name == ":status").Value, out int s) ? s : 0;
        string enc = respHdrs.LastOrDefault(h => h.Name.Equals("content-encoding", StringComparison.OrdinalIgnoreCase)).Value ?? "";
        byte[] finalBody = await TlsHelpers.DecompressAsync(bodyMs, enc);

        var resp = new HttpResponseMessage((HttpStatusCode)sc) { RequestMessage = orig, Content = new ByteArrayContent(finalBody) };
        foreach (var (k, v) in respHdrs)
        {
            if (k.StartsWith(":")) continue;
            // Body is already decompressed; strip transfer-related headers so callers see correct Content-Length.
            if (k.Equals("content-encoding", StringComparison.OrdinalIgnoreCase)) continue;
            if (k.Equals("content-length",   StringComparison.OrdinalIgnoreCase)) continue;
            if (!resp.Headers.TryAddWithoutValidation(k, v)) resp.Content.Headers.TryAddWithoutValidation(k, v);
        }
        return resp;
    }

    // Overload: send a slice of a larger array without allocating an intermediate copy.
    // Used by body chunking in SendRequestAsync to avoid body[offset..offset+chunkSize].
    private async Task WriteFrameAsync(byte type, byte flags, int sid,
        byte[] payload, int payloadOffset, int payloadLen, CancellationToken ct)
    {
        int s2 = sid & 0x7FFFFFFF;

        if (payloadLen <= _serverMaxFrameSize)
        {
            int total = 9 + payloadLen;
            byte[] f = ArrayPool<byte>.Shared.Rent(total);
            try
            {
                f[0] = (byte)(payloadLen >> 16); f[1] = (byte)(payloadLen >> 8); f[2] = (byte)(payloadLen & 0xFF);
                f[3] = type; f[4] = flags;
                f[5] = (byte)(s2 >> 24); f[6] = (byte)(s2 >> 16); f[7] = (byte)(s2 >> 8); f[8] = (byte)(s2 & 0xFF);
                if (payloadLen > 0) Buffer.BlockCopy(payload, payloadOffset, f, 9, payloadLen);
                await _stream.WriteAsync(f.AsMemory(0, total), ct);
            }
            finally { ArrayPool<byte>.Shared.Return(f); }
            return;
        }

        // Fragmentation path (mirrors the main overload)
        int localOffset = 0;
        bool firstChunk = true;
        while (localOffset < payloadLen)
        {
            int chunkLen = Math.Min(_serverMaxFrameSize, payloadLen - localOffset);
            bool lastChunk = (localOffset + chunkLen) >= payloadLen;
            byte chunkType, chunkFlags;
            if (type == 0x0) { chunkType = 0x0; chunkFlags = lastChunk ? flags : (byte)(flags & ~0x1); }
            else if (type == 0x1)
            {
                if (firstChunk) { chunkType = 0x1; chunkFlags = lastChunk ? flags : (byte)(flags & ~0x4); }
                else            { chunkType = 0x9; chunkFlags = lastChunk ? (byte)0x4 : (byte)0x0; }
            }
            else throw new HttpRequestException($"H2 frame type 0x{type:x} payload {payloadLen} B exceeds MAX_FRAME_SIZE {_serverMaxFrameSize} B");

            byte[] f = ArrayPool<byte>.Shared.Rent(9 + chunkLen);
            try
            {
                f[0] = (byte)(chunkLen >> 16); f[1] = (byte)(chunkLen >> 8); f[2] = (byte)(chunkLen & 0xFF);
                f[3] = chunkType; f[4] = chunkFlags;
                f[5] = (byte)(s2 >> 24); f[6] = (byte)(s2 >> 16); f[7] = (byte)(s2 >> 8); f[8] = (byte)(s2 & 0xFF);
                Buffer.BlockCopy(payload, payloadOffset + localOffset, f, 9, chunkLen);
                await _stream.WriteAsync(f.AsMemory(0, 9 + chunkLen), ct);
            }
            finally { ArrayPool<byte>.Shared.Return(f); }
            localOffset += chunkLen;
            firstChunk = false;
        }
    }

    private async Task WriteFrameAsync(byte type, byte flags, int sid, byte[] payload, CancellationToken ct)
    {
        int len = payload?.Length ?? 0;
        int s2 = sid & 0x7FFFFFFF;

        if (len <= _serverMaxFrameSize)
        {
            // Common path: payload fits in a single frame — rent from pool to avoid heap allocs
            int total = 9 + len;
            byte[] f = ArrayPool<byte>.Shared.Rent(total);
            try
            {
                f[0] = (byte)(len >> 16); f[1] = (byte)(len >> 8); f[2] = (byte)(len & 0xFF);
                f[3] = type; f[4] = flags;
                f[5] = (byte)(s2 >> 24); f[6] = (byte)(s2 >> 16); f[7] = (byte)(s2 >> 8); f[8] = (byte)(s2 & 0xFF);
                if (len > 0) Buffer.BlockCopy(payload, 0, f, 9, len);
                await _stream.WriteAsync(f.AsMemory(0, total), ct);
            }
            finally { ArrayPool<byte>.Shared.Return(f); }
            return;
        }

        // Payload exceeds MAX_FRAME_SIZE — fragment (RFC 7540 §4.1, §6.1, §6.2, §6.10)
        int offset = 0;
        bool firstChunk = true;
        while (offset < len)
        {
            int chunkLen = Math.Min(_serverMaxFrameSize, len - offset);
            bool lastChunk = (offset + chunkLen) >= len;
            byte chunkType, chunkFlags;
            if (type == 0x0) // DATA: split into multiple DATA frames
            {
                chunkType = 0x0;
                chunkFlags = lastChunk ? flags : (byte)(flags & ~0x1); // END_STREAM only on last
            }
            else if (type == 0x1) // HEADERS: first=HEADERS, rest=CONTINUATION frames
            {
                if (firstChunk)
                {
                    chunkType = 0x1;
                    chunkFlags = lastChunk ? flags : (byte)(flags & ~0x4); // clear END_HEADERS if not last
                }
                else
                {
                    chunkType = 0x9; // CONTINUATION
                    chunkFlags = lastChunk ? (byte)0x4 : (byte)0x0; // END_HEADERS only on last
                }
            }
            else
            {
                throw new HttpRequestException($"H2 frame type 0x{type:x} payload {len} B exceeds MAX_FRAME_SIZE {_serverMaxFrameSize} B");
            }
            byte[] f = ArrayPool<byte>.Shared.Rent(9 + chunkLen);
            try
            {
                f[0] = (byte)(chunkLen >> 16); f[1] = (byte)(chunkLen >> 8); f[2] = (byte)(chunkLen & 0xFF);
                f[3] = chunkType; f[4] = chunkFlags;
                f[5] = (byte)(s2 >> 24); f[6] = (byte)(s2 >> 16); f[7] = (byte)(s2 >> 8); f[8] = (byte)(s2 & 0xFF);
                Buffer.BlockCopy(payload, offset, f, 9, chunkLen);
                await _stream.WriteAsync(f.AsMemory(0, 9 + chunkLen), ct);
            }
            finally { ArrayPool<byte>.Shared.Return(f); }
            offset += chunkLen;
            firstChunk = false;
        }
    }

    private async Task<(byte, byte, int, byte[])> ReadFrameAsync(CancellationToken ct)
    {
        // Read directly into _frameHeader (9 bytes), then allocate a fresh buffer for the payload.
        // This avoids the previous bug where ReadExactAsync(9) returned _frameHeader for both the
        // header and a 9-byte payload, causing the payload array to be silently overwritten on the next call.
        await ReadExactIntoAsync(_frameHeader, ct);
        byte fType  = _frameHeader[3];
        byte fFlags = _frameHeader[4];
        int len = (_frameHeader[0] << 16) | (_frameHeader[1] << 8) | _frameHeader[2];
        int sid = ((_frameHeader[5] & 0x7F) << 24) | (_frameHeader[6] << 16) | (_frameHeader[7] << 8) | _frameHeader[8];
        byte[] payload = len > 0 ? await ReadExactAsync(len, ct) : Array.Empty<byte>();
        return (fType, fFlags, sid, payload);
    }

    private readonly byte[] _frameHeader = new byte[9];

    // Fills an existing buffer — used only for the 9-byte frame header to avoid allocation.
    private async Task ReadExactIntoAsync(byte[] buf, CancellationToken ct)
    {
        int off = 0, count = buf.Length;
        while (off < count) { int r = await _stream.ReadAsync(buf, off, count - off, ct); if (r == 0) throw new EndOfStreamException("HTTP/2 closed"); off += r; }
    }

    // Always allocates a fresh buffer — used for frame payloads.
    private async Task<byte[]> ReadExactAsync(int count, CancellationToken ct)
    {
        var buf = new byte[count];
        int off = 0;
        while (off < count) { int r = await _stream.ReadAsync(buf, off, count - off, ct); if (r == 0) throw new EndOfStreamException("HTTP/2 closed"); off += r; }
        return buf;
    }

    private static byte[] BuildSettingsPayload(params (int Id, int Value)[] entries)
    {
        using var ms = new MemoryStream();
        foreach (var (id, val) in entries)
        {
            ms.WriteByte((byte)(id >> 8)); ms.WriteByte((byte)(id & 0xFF));
            ms.WriteByte((byte)(val >> 24)); ms.WriteByte((byte)(val >> 16));
            ms.WriteByte((byte)(val >> 8)); ms.WriteByte((byte)(val & 0xFF));
        }
        return ms.ToArray();
    }

    private static byte[] BigEndian4(int v)
        => new[] { (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)(v & 0xFF) };

    // Stateful HPACK encoder — mirrors the dynamic table so repeated headers are sent as indexed
    // references (0x80|idx) on requests 2+ over the same pooled H2 connection, matching Chrome's behavior.
    private sealed class HpackEncoderState
    {
        private readonly LinkedList<(string Name, string Value)> _dyn = new();
        private int _dynTableSize;
        private int _maxSize = 4096;
        private int _pendingMinSize = -1; // RFC 7541 §4.2: smallest size seen between header blocks
        private int _pendingMaxSize = -1; // final size to signal; ≥ 0 → emit size updates at start of next Encode()

        // Called by ApplyServerSettings when server sends SETTINGS_HEADER_TABLE_SIZE (id=1).
        public void SetMaxSize(int newMax)
        {
            if (newMax == _maxSize && _pendingMaxSize < 0) return;
            // RFC 7541 §4.2: if multiple size changes occur between header blocks, the SMALLEST intermediate
            // value and the FINAL value must both be signaled to the decoder.
            if (_pendingMinSize < 0 || newMax < _pendingMinSize) _pendingMinSize = newMax;
            _pendingMaxSize = newMax;
            _maxSize = newMax;
            Evict();
        }

        public byte[] Encode(IEnumerable<(string Name, string Value)> headers)
        {
            using var ms = new MemoryStream();

            // RFC 7541 §4.2: emit dynamic table size update(s) at the beginning of the first header block
            // following a change to the maximum size. When sizes changed multiple times, emit the minimum
            // first (so the decoder evicts entries) then the final value.
            if (_pendingMaxSize >= 0)
            {
                if (_pendingMinSize >= 0 && _pendingMinSize != _pendingMaxSize)
                    HpackWriteInt(ms, _pendingMinSize, 5, 0x20); // intermediate minimum
                HpackWriteInt(ms, _pendingMaxSize, 5, 0x20);     // final value
                _pendingMinSize = -1;
                _pendingMaxSize = -1;
            }

            foreach (var (name, value) in headers)
            {
                // 1. Full static table match → single indexed byte (smallest possible encoding)
                int sidx = HpackStaticFullIndex(name, value);
                if (sidx > 0) { ms.WriteByte((byte)(0x80 | sidx)); continue; }

                // 2. Full dynamic table match → single indexed byte
                int dfull = FindDynFull(name, value);
                if (dfull > 0) { HpackWriteInt(ms, dfull, 7, 0x80); continue; }

                var vHuff = HpackHuffman.Encode(Encoding.Latin1.GetBytes(value));

                // 3. Dynamic name-only match → literal incremental, reuse dynamic name index
                int dname = FindDynName(name);
                if (dname > 0)
                {
                    HpackWriteInt(ms, dname, 6, 0x40);
                    HpackWriteLen(ms, vHuff.Length, huffman: true);
                    ms.Write(vHuff, 0, vHuff.Length);
                    AddEntry(name, value);
                    continue;
                }

                // 4. Static name match → literal incremental, static name index
                int nidx = HpackStaticNameIndex(name);
                if (nidx > 0)
                    HpackWriteInt(ms, nidx, 6, 0x40);
                else
                {
                    ms.WriteByte(0x40);
                    var nHuff = HpackHuffman.Encode(Encoding.Latin1.GetBytes(name));
                    HpackWriteLen(ms, nHuff.Length, huffman: true);
                    ms.Write(nHuff, 0, nHuff.Length);
                }
                HpackWriteLen(ms, vHuff.Length, huffman: true);
                ms.Write(vHuff, 0, vHuff.Length);
                AddEntry(name, value);
            }
            return ms.ToArray();
        }

        // Returns dynamic table index (62-based, most-recently-added = 62) for exact name+value match.
        private int FindDynFull(string name, string value)
        {
            int i = 0;
            foreach (var e in _dyn) { if (e.Name == name && e.Value == value) return 62 + i; i++; }
            return 0;
        }

        // Returns dynamic table index (62-based) for first entry whose name matches.
        private int FindDynName(string name)
        {
            int i = 0;
            foreach (var e in _dyn) { if (e.Name == name) return 62 + i; i++; }
            return 0;
        }

        private void AddEntry(string name, string value)
        {
            int sz = name.Length + value.Length + 32;
            Evict(sz);
            if (sz <= _maxSize) { _dyn.AddFirst((name, value)); _dynTableSize += sz; }
        }

        private void Evict(int needed = 0)
        {
            while (_dyn.Count > 0 && _dynTableSize + needed > _maxSize)
            {
                var old = _dyn.Last.Value;
                _dynTableSize -= old.Name.Length + old.Value.Length + 32;
                _dyn.RemoveLast();
            }
        }
    }

    // 7-bit HPACK string length with Huffman flag (bit 7 of first byte)
    private static void HpackWriteLen(MemoryStream ms, int v, bool huffman)
    {
        byte hbit = huffman ? (byte)0x80 : (byte)0;
        if (v < 127) { ms.WriteByte((byte)(hbit | v)); return; }
        ms.WriteByte((byte)(hbit | 0x7F)); v -= 127;
        while (v >= 128) { ms.WriteByte((byte)((v & 0x7F) | 0x80)); v >>= 7; }
        ms.WriteByte((byte)v);
    }

    // N-bit HPACK integer with flag nibble (for literal header field formats)
    private static void HpackWriteInt(MemoryStream ms, int v, int prefixBits, byte flagByte)
    {
        int max = (1 << prefixBits) - 1;
        if (v < max) { ms.WriteByte((byte)(flagByte | v)); return; }
        ms.WriteByte((byte)(flagByte | max)); v -= max;
        while (v >= 128) { ms.WriteByte((byte)((v & 0x7F) | 0x80)); v >>= 7; }
        ms.WriteByte((byte)v);
    }

    // RFC 7541 §2.3.2 static table — full name+value match (all 61 entries)
    private static int HpackStaticFullIndex(string n, string v) => (n, v) switch
    {
        (":authority",                  "")              => 1,
        (":method",                     "GET")           => 2,
        (":method",                     "POST")          => 3,
        (":path",                       "/")             => 4,
        (":path",                       "/index.html")   => 5,
        (":scheme",                     "http")          => 6,
        (":scheme",                     "https")         => 7,
        (":status",                     "200")           => 8,
        (":status",                     "204")           => 9,
        (":status",                     "206")           => 10,
        (":status",                     "304")           => 11,
        (":status",                     "400")           => 12,
        (":status",                     "404")           => 13,
        (":status",                     "500")           => 14,
        ("accept-charset",              "")              => 15,
        ("accept-encoding",             "gzip, deflate") => 16,
        ("accept-language",             "")              => 17,
        ("accept-ranges",               "")              => 18,
        ("accept",                      "")              => 19,
        ("access-control-allow-origin", "")              => 20,
        ("age",                         "")              => 21,
        ("allow",                       "")              => 22,
        ("authorization",               "")              => 23,
        ("cache-control",               "")              => 24,
        ("content-disposition",         "")              => 25,
        ("content-encoding",            "")              => 26,
        ("content-language",            "")              => 27,
        ("content-length",              "")              => 28,
        ("content-location",            "")              => 29,
        ("content-range",               "")              => 30,
        ("content-type",                "")              => 31,
        ("cookie",                      "")              => 32,
        ("date",                        "")              => 33,
        ("etag",                        "")              => 34,
        ("expect",                      "")              => 35,
        ("expires",                     "")              => 36,
        ("from",                        "")              => 37,
        ("host",                        "")              => 38,
        ("if-match",                    "")              => 39,
        ("if-modified-since",           "")              => 40,
        ("if-none-match",               "")              => 41,
        ("if-range",                    "")              => 42,
        ("if-unmodified-since",         "")              => 43,
        ("last-modified",               "")              => 44,
        ("link",                        "")              => 45,
        ("location",                    "")              => 46,
        ("max-forwards",                "")              => 47,
        ("proxy-authenticate",          "")              => 48,
        ("proxy-authorization",         "")              => 49,
        ("range",                       "")              => 50,
        ("referer",                     "")              => 51,
        ("refresh",                     "")              => 52,
        ("retry-after",                 "")              => 53,
        ("server",                      "")              => 54,
        ("set-cookie",                  "")              => 55,
        ("strict-transport-security",   "")              => 56,
        ("transfer-encoding",           "")              => 57,
        ("user-agent",                  "")              => 58,
        ("vary",                        "")              => 59,
        ("via",                         "")              => 60,
        ("www-authenticate",            "")              => 61,
        _ => 0
    };

    // RFC 7541 §2.3.2 static table — name-only index (all 61 entries)
    private static int HpackStaticNameIndex(string n) => n switch
    {
        ":authority"                  => 1,  ":method"               => 2,  ":path"                 => 4,
        ":scheme"                     => 6,  ":status"               => 8,  "accept-charset"        => 15,
        "accept-encoding"             => 16, "accept-language"       => 17, "accept-ranges"         => 18,
        "accept"                      => 19, "access-control-allow-origin" => 20, "age"             => 21,
        "allow"                       => 22, "authorization"         => 23, "cache-control"         => 24,
        "content-disposition"         => 25, "content-encoding"      => 26, "content-language"      => 27,
        "content-length"              => 28, "content-location"      => 29, "content-range"         => 30,
        "content-type"                => 31, "cookie"                => 32, "date"                  => 33,
        "etag"                        => 34, "expect"                => 35, "expires"               => 36,
        "from"                        => 37, "host"                  => 38, "if-match"              => 39,
        "if-modified-since"           => 40, "if-none-match"         => 41, "if-range"              => 42,
        "if-unmodified-since"         => 43, "last-modified"         => 44, "link"                  => 45,
        "location"                    => 46, "max-forwards"          => 47, "proxy-authenticate"    => 48,
        "proxy-authorization"         => 49, "range"                 => 50, "referer"               => 51,
        "refresh"                     => 52, "retry-after"           => 53, "server"                => 54,
        "set-cookie"                  => 55, "strict-transport-security" => 56, "transfer-encoding" => 57,
        "user-agent"                  => 58, "vary"                  => 59, "via"                   => 60,
        "www-authenticate"            => 61,
        _ => 0
    };

}

// ─────────────────────────────────────────────────────────────────────────────
// RFC 7541 Appendix B Huffman encoder — complete 256-symbol table
// ─────────────────────────────────────────────────────────────────────────────
internal static class HpackHuffman
{
    // (code, bit_length) indexed by byte value 0-255
    private static readonly (uint Code, int Bits)[] Table =
    {
        // 0-7
        (0x1ff8,13),(0x7fffd8,23),(0xfffffe2,28),(0xfffffe3,28),(0xfffffe4,28),(0xfffffe5,28),(0xfffffe6,28),(0xfffffe7,28),
        // 8-15
        (0xfffffe8,28),(0xffffea,24),(0x3ffffffc,30),(0xfffffe9,28),(0xfffffea,28),(0x3ffffffd,30),(0xfffffeb,28),(0xfffffec,28),
        // 16-23
        (0xfffffed,28),(0xfffffee,28),(0xfffffef,28),(0xffffff0,28),(0xffffff1,28),(0xffffff2,28),(0x3ffffffe,30),(0xffffff3,28),
        // 24-31
        (0xffffff4,28),(0xffffff5,28),(0xffffff6,28),(0xffffff7,28),(0xffffff8,28),(0xffffff9,28),(0xffffffa,28),(0xffffffb,28),
        // 32-39: ' ' '!' '"' '#' '$' '%' '&' '\''
        (0x14,6),(0x3f8,10),(0x3f9,10),(0xffa,12),(0x1ff9,13),(0x15,6),(0xf8,8),(0x7fa,11),
        // 40-47: '(' ')' '*' '+' ',' '-' '.' '/'
        (0x3fa,10),(0x3fb,10),(0xf9,8),(0x7fb,11),(0xfa,8),(0x16,6),(0x17,6),(0x18,6),
        // 48-55: '0'-'7'
        (0x0,5),(0x1,5),(0x2,5),(0x19,6),(0x1a,6),(0x1b,6),(0x1c,6),(0x1d,6),
        // 56-63: '8' '9' ':' ';' '<' '=' '>' '?'
        (0x1e,6),(0x1f,6),(0x5c,7),(0xfb,8),(0x7ffc,15),(0x20,6),(0xffb,12),(0x3fc,10),
        // 64-71: '@' 'A'-'G'
        (0x1ffa,13),(0x21,6),(0x5d,7),(0x5e,7),(0x5f,7),(0x60,7),(0x61,7),(0x62,7),
        // 72-79: 'H'-'O'
        (0x63,7),(0x64,7),(0x65,7),(0x66,7),(0x67,7),(0x68,7),(0x69,7),(0x6a,7),
        // 80-87: 'P'-'W'
        (0x6b,7),(0x6c,7),(0x6d,7),(0x6e,7),(0x6f,7),(0x70,7),(0x71,7),(0x72,7),
        // 88-95: 'X'-'_'
        (0xfc,8),(0x73,7),(0xfd,8),(0x1ffb,13),(0x7fff0,19),(0x1ffc,13),(0x3ffc,14),(0x22,6),
        // 96-103: '`' 'a'-'g'
        (0x7ffd,15),(0x3,5),(0x23,6),(0x4,5),(0x24,6),(0x5,5),(0x25,6),(0x26,6),
        // 104-111: 'h'-'o'
        (0x27,6),(0x6,5),(0x74,7),(0x75,7),(0x28,6),(0x29,6),(0x2a,6),(0x7,5),
        // 112-119: 'p'-'w'
        (0x2b,6),(0x76,7),(0x2c,6),(0x8,5),(0x9,5),(0x2d,6),(0x77,7),(0x78,7),
        // 120-127: 'x'-DEL
        (0x79,7),(0x7a,7),(0x7b,7),(0x7ffe,15),(0x7fc,11),(0x3ffd,14),(0x1ffd,13),(0xffffffc,28),
        // 128-135
        (0xfffe6,20),(0x3fffd2,22),(0xfffe7,20),(0xfffe8,20),(0x3fffd3,22),(0x3fffd4,22),(0x3fffd5,22),(0x7fffd9,23),
        // 136-143
        (0x3fffd6,22),(0x7fffda,23),(0x7fffdb,23),(0x7fffdc,23),(0x7fffdd,23),(0x7fffde,23),(0xffffeb,24),(0x7fffdf,23),
        // 144-151
        (0xffffec,24),(0xffffed,24),(0x3fffd7,22),(0x7fffe0,23),(0xffffee,24),(0x7fffe1,23),(0x7fffe2,23),(0x7fffe3,23),
        // 152-159
        (0x7fffe4,23),(0x1fffdc,21),(0x3fffd8,22),(0x7fffe5,23),(0x3fffd9,22),(0x7fffe6,23),(0x7fffe7,23),(0xffffef,24),
        // 160-167
        (0x3fffda,22),(0x1fffdd,21),(0xfffe9,20),(0x3fffdb,22),(0x3fffdc,22),(0x7fffe8,23),(0x7fffe9,23),(0x1fffde,21),
        // 168-175
        (0x7fffea,23),(0x3fffdd,22),(0x3fffde,22),(0xfffff0,24),(0x1fffdf,21),(0x3fffdf,22),(0x7fffeb,23),(0x7fffec,23),
        // 176-183
        (0x1fffe0,21),(0x1fffe1,21),(0x3fffe0,22),(0x1fffe2,21),(0x7fffed,23),(0x3fffe1,22),(0x7fffee,23),(0x7fffef,23),
        // 184-191
        (0xfffea,20),(0x3fffe2,22),(0x3fffe3,22),(0x3fffe4,22),(0x7ffff0,23),(0x3fffe5,22),(0x3fffe6,22),(0x7ffff1,23),
        // 192-199
        (0x3ffffe0,26),(0x3ffffe1,26),(0xfffeb,20),(0x7fff1,19),(0x3fffe7,22),(0x7ffff2,23),(0x3fffe8,22),(0x1fffec,21),
        // 200-207
        (0x3fffe9,22),(0x1fffed,21),(0x1fffe3,21),(0x3fffea,22),(0x7ffff3,23),(0x3fffeb,22),(0x7ffff4,23),(0xfffff1,24),
        // 208-215
        (0xfffff2,24),(0x1fffe4,21),(0x1fffe5,21),(0x3fffec,22),(0xfffff3,24),(0x3fffed,22),(0x7ffff5,23),(0xfffff4,24),
        // 216-223
        (0xfffff5,24),(0x3ffffe2,26),(0xfffff6,24),(0x3ffffe3,26),(0x3ffffe4,26),(0x7ffffde,27),(0x7ffffdf,27),(0x3ffffe5,26),
        // 224-231
        (0xfffff7,24),(0x7ffffe0,27),(0x7ffffe1,27),(0x3ffffe6,26),(0x7ffffe2,27),(0xfffff8,24),(0xfffff9,24),(0x7ffffe3,27),
        // 232-239
        (0x7ffffe4,27),(0x7ffffe5,27),(0xffffffd,28),(0x7ffffe6,27),(0x7ffffe7,27),(0x7ffffe8,27),(0x7ffffe9,27),(0x7ffffea,27),
        // 240-247
        (0x7ffffeb,27),(0xffffffe,28),(0x7ffffec,27),(0x7ffffed,27),(0x7ffffee,27),(0x7ffffef,27),(0x7fffff0,27),(0x3ffffe7,26),
        // 248-255
        (0x7fffff1,27),(0x3ffffe8,26),(0x1ffffffc,29),(0x3ffffe9,26),(0x3ffffea,26),(0x7fffff2,27),(0x3ffffeb,26),(0x7fffff3,27),
    };

    // Bit-pack all bytes using their Huffman codes; pad final byte with 1s (EOS)
    public static byte[] Encode(byte[] input)
    {
        if (input.Length == 0) return Array.Empty<byte>();
        int maxBytes = (input.Length * 30 + 7) / 8 + 1;
        var rent = ArrayPool<byte>.Shared.Rent(maxBytes);
        try
        {
            int pos = 0; ulong buf = 0UL; int bits = 0;
            foreach (byte b in input)
            {
                var (code, len) = Table[b];
                buf = (buf << len) | code;
                bits += len;
                while (bits >= 8) { bits -= 8; rent[pos++] = (byte)(buf >> bits); }
            }
            if (bits > 0)
                rent[pos++] = (byte)((buf << (8 - bits)) | (uint)((1 << (8 - bits)) - 1));
            return rent[..pos].ToArray();
        }
        finally { ArrayPool<byte>.Shared.Return(rent); }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// HPACK decoder — indexed + literal header field support (RFC 7541)
// Includes full Huffman decoding (RFC 7541 Appendix B)
// ─────────────────────────────────────────────────────────────────────────────
internal sealed class HpackDecoder
{
    private static readonly (string Name, string Value)[] StaticTable =
    {
        ("",""),  (":authority",""),
        (":method","GET"),(":method","POST"),(":path","/"),(":path","/index.html"),
        (":scheme","http"),(":scheme","https"),(":status","200"),(":status","204"),
        (":status","206"),(":status","304"),(":status","400"),(":status","404"),
        (":status","500"),("accept-charset",""),("accept-encoding","gzip, deflate"),
        ("accept-language",""),("accept-ranges",""),("accept",""),
        ("access-control-allow-origin",""),("age",""),("allow",""),("authorization",""),
        ("cache-control",""),("content-disposition",""),("content-encoding",""),
        ("content-language",""),("content-length",""),("content-location",""),
        ("content-range",""),("content-type",""),("cookie",""),("date",""),
        ("etag",""),("expect",""),("expires",""),("from",""),("host",""),
        ("if-match",""),("if-modified-since",""),("if-none-match",""),("if-range",""),
        ("if-unmodified-since",""),("last-modified",""),("link",""),("location",""),
        ("max-forwards",""),("proxy-authenticate",""),("proxy-authorization",""),
        ("range",""),("referer",""),("refresh",""),("retry-after",""),("server",""),
        ("set-cookie",""),("strict-transport-security",""),("transfer-encoding",""),
        ("user-agent",""),("vary",""),("via",""),("www-authenticate",""),
    };

    // RFC 7541 Appendix B Huffman decode table — complete 256-symbol set
    private static readonly Dictionary<(uint, int), int> HuffMap;
    static HpackDecoder()
    {
        // Complete RFC 7541 Appendix B table: (code, bit-length, symbol)
        var tab = new (uint C, int L, int S)[]
        {
            // Symbols 0-31 (control characters — long codes, rarely appear in HTTP)
            (0x1ff8,13,0),(0x7fffd8,23,1),(0xfffffe2,28,2),(0xfffffe3,28,3),
            (0xfffffe4,28,4),(0xfffffe5,28,5),(0xfffffe6,28,6),(0xfffffe7,28,7),
            (0xfffffe8,28,8),(0xffffea,24,9),(0x3ffffffc,30,10),(0xfffffe9,28,11),
            (0xfffffea,28,12),(0x3ffffffd,30,13),(0xfffffeb,28,14),(0xfffffec,28,15),
            (0xfffffed,28,16),(0xfffffee,28,17),(0xfffffef,28,18),(0xffffff0,28,19),
            (0xffffff1,28,20),(0xffffff2,28,21),(0x3ffffffe,30,22),(0xffffff3,28,23),
            (0xffffff4,28,24),(0xffffff5,28,25),(0xffffff6,28,26),(0xffffff7,28,27),
            (0xffffff8,28,28),(0xffffff9,28,29),(0xffffffa,28,30),(0xffffffb,28,31),

            // Symbols 32-126 (printable ASCII — RFC 7541 Appendix B)
            (0x14,6,32),(0x3f8,10,33),(0x3f9,10,34),(0xffa,12,35),(0x1ff9,13,36),(0x15,6,37),(0xf8,8,38),(0x7fa,11,39),
            (0x3fa,10,40),(0x3fb,10,41),(0xf9,8,42),(0x7fb,11,43),(0xfa,8,44),(0x16,6,45),(0x17,6,46),(0x18,6,47),
            (0x0,5,48),(0x1,5,49),(0x2,5,50),(0x19,6,51),(0x1a,6,52),(0x1b,6,53),(0x1c,6,54),(0x1d,6,55),
            (0x1e,6,56),(0x1f,6,57),(0x5c,7,58),(0xfb,8,59),(0x7ffc,15,60),(0x20,6,61),(0xffb,12,62),(0x3fc,10,63),
            (0x1ffa,13,64),(0x21,6,65),(0x5d,7,66),(0x5e,7,67),(0x5f,7,68),(0x60,7,69),(0x61,7,70),(0x62,7,71),
            (0x63,7,72),(0x64,7,73),(0x65,7,74),(0x66,7,75),(0x67,7,76),(0x68,7,77),(0x69,7,78),(0x6a,7,79),
            (0x6b,7,80),(0x6c,7,81),(0x6d,7,82),(0x6e,7,83),(0x6f,7,84),(0x70,7,85),(0x71,7,86),(0x72,7,87),
            (0xfc,8,88),(0x73,7,89),(0xfd,8,90),(0x1ffb,13,91),(0x7fff0,19,92),(0x1ffc,13,93),(0x3ffc,14,94),(0x22,6,95),
            (0x7ffd,15,96),(0x3,5,97),(0x23,6,98),(0x4,5,99),(0x24,6,100),(0x5,5,101),(0x25,6,102),(0x26,6,103),
            (0x27,6,104),(0x6,5,105),(0x74,7,106),(0x75,7,107),(0x28,6,108),(0x29,6,109),(0x2a,6,110),(0x7,5,111),
            (0x2b,6,112),(0x76,7,113),(0x2c,6,114),(0x8,5,115),(0x9,5,116),(0x2d,6,117),(0x77,7,118),(0x78,7,119),
            (0x79,7,120),(0x7a,7,121),(0x7b,7,122),(0x7ffe,15,123),(0x7fc,11,124),(0x3ffd,14,125),(0x1ffd,13,126),

            // Symbols 127-255 (RFC 7541 Appendix B)
            (0xffffffc,28,127),
            (0xfffe6,20,128),(0x3fffd2,22,129),(0xfffe7,20,130),(0xfffe8,20,131),(0x3fffd3,22,132),(0x3fffd4,22,133),(0x3fffd5,22,134),(0x7fffd9,23,135),
            (0x3fffd6,22,136),(0x7fffda,23,137),(0x7fffdb,23,138),(0x7fffdc,23,139),(0x7fffdd,23,140),(0x7fffde,23,141),(0xffffeb,24,142),(0x7fffdf,23,143),
            (0xffffec,24,144),(0xffffed,24,145),(0x3fffd7,22,146),(0x7fffe0,23,147),(0xffffee,24,148),(0x7fffe1,23,149),(0x7fffe2,23,150),(0x7fffe3,23,151),
            (0x7fffe4,23,152),(0x1fffdc,21,153),(0x3fffd8,22,154),(0x7fffe5,23,155),(0x3fffd9,22,156),(0x7fffe6,23,157),(0x7fffe7,23,158),(0xffffef,24,159),
            (0x3fffda,22,160),(0x1fffdd,21,161),(0xfffe9,20,162),(0x3fffdb,22,163),(0x3fffdc,22,164),(0x7fffe8,23,165),(0x7fffe9,23,166),(0x1fffde,21,167),
            (0x7fffea,23,168),(0x3fffdd,22,169),(0x3fffde,22,170),(0xfffff0,24,171),(0x1fffdf,21,172),(0x3fffdf,22,173),(0x7fffeb,23,174),(0x7fffec,23,175),
            (0x1fffe0,21,176),(0x1fffe1,21,177),(0x3fffe0,22,178),(0x1fffe2,21,179),(0x7fffed,23,180),(0x3fffe1,22,181),(0x7fffee,23,182),(0x7fffef,23,183),
            (0xfffea,20,184),(0x3fffe2,22,185),(0x3fffe3,22,186),(0x3fffe4,22,187),(0x7ffff0,23,188),(0x3fffe5,22,189),(0x3fffe6,22,190),(0x7ffff1,23,191),
            (0x3ffffe0,26,192),(0x3ffffe1,26,193),(0xfffeb,20,194),(0x7fff1,19,195),(0x3fffe7,22,196),(0x7ffff2,23,197),(0x3fffe8,22,198),(0x1fffec,21,199),
            (0x3fffe9,22,200),(0x1fffed,21,201),(0x1fffe3,21,202),(0x3fffea,22,203),(0x7ffff3,23,204),(0x3fffeb,22,205),(0x7ffff4,23,206),(0xfffff1,24,207),
            (0xfffff2,24,208),(0x1fffe4,21,209),(0x1fffe5,21,210),(0x3fffec,22,211),(0xfffff3,24,212),(0x3fffed,22,213),(0x7ffff5,23,214),(0xfffff4,24,215),
            (0xfffff5,24,216),(0x3ffffe2,26,217),(0xfffff6,24,218),(0x3ffffe3,26,219),(0x3ffffe4,26,220),(0x7ffffde,27,221),(0x7ffffdf,27,222),(0x3ffffe5,26,223),
            (0xfffff7,24,224),(0x7ffffe0,27,225),(0x7ffffe1,27,226),(0x3ffffe6,26,227),(0x7ffffe2,27,228),(0xfffff8,24,229),(0xfffff9,24,230),(0x7ffffe3,27,231),
            (0x7ffffe4,27,232),(0x7ffffe5,27,233),(0xffffffd,28,234),(0x7ffffe6,27,235),(0x7ffffe7,27,236),(0x7ffffe8,27,237),(0x7ffffe9,27,238),(0x7ffffea,27,239),
            (0x7ffffeb,27,240),(0xffffffe,28,241),(0x7ffffec,27,242),(0x7ffffed,27,243),(0x7ffffee,27,244),(0x7ffffef,27,245),(0x7fffff0,27,246),(0x3ffffe7,26,247),
            (0x7fffff1,27,248),(0x3ffffe8,26,249),(0x1ffffffc,29,250),(0x3ffffe9,26,251),(0x3ffffea,26,252),(0x7fffff2,27,253),(0x3ffffeb,26,254),(0x7fffff3,27,255),

        };
        HuffMap = new Dictionary<(uint, int), int>(tab.Length);
        foreach (var (c, l, s) in tab) HuffMap[(c, l)] = s;
    }

    private static string HuffmanDecode(byte[] data, int offset, int count)
    {
        var sb = new StringBuilder();
        ulong bits = 0;
        int avail = 0, idx = offset, end = offset + count;
        while (idx < end || avail >= 5)
        {
            while (avail < 30 && idx < end) { bits = (bits << 8) | data[idx++]; avail += 8; }
            if (avail < 5) break;
            bool found = false;
            int maxLen = Math.Min(30, avail);
            for (int len = 5; len <= maxLen; len++)
            {
                uint code = (uint)(bits >> (avail - len)) & ((1u << len) - 1u);
                if (HuffMap.TryGetValue((code, len), out int sym))
                {
                    sb.Append((char)sym);
                    avail -= len;
                    found = true;
                    break;
                }
            }
            if (!found) break; // EOS padding or unknown symbol
        }
        return sb.ToString();
    }

    // LinkedList: AddFirst/RemoveLast are O(1) vs List.Insert(0) which is O(N).
    // Lookup.ElementAt(di) is O(N) but occurs less frequently than header insertions.
    private readonly LinkedList<(string Name, string Value)> _dyn = new();
    private int _dynMaxSize = 4096;
    private int _dynTableSize = 0; // RFC 7541 §4.1: sum of (name.len + value.len + 32) for each entry

    private void AddDynEntry(string name, string val)
    {
        int sz = name.Length + val.Length + 32; // RFC 7541 §4.1 overhead per entry
        while (_dynTableSize + sz > _dynMaxSize && _dyn.Count > 0)
        {
            var old = _dyn.Last.Value;
            _dynTableSize -= old.Name.Length + old.Value.Length + 32;
            _dyn.RemoveLast();
        }
        if (sz <= _dynMaxSize) { _dyn.AddFirst((name, val)); _dynTableSize += sz; }
    }

    private void EvictToSize(int maxSize)
    {
        while (_dynTableSize > maxSize && _dyn.Count > 0)
        {
            var old = _dyn.Last.Value;
            _dynTableSize -= old.Name.Length + old.Value.Length + 32;
            _dyn.RemoveLast();
        }
    }

    public List<(string Name, string Value)> Decode(byte[] data)
    {
        var result = new List<(string Name, string Value)>();
        int pos = 0;
        while (pos < data.Length)
        {
            byte b = data[pos];
            if ((b & 0x80) != 0) { result.Add(Lookup(ReadInt(data, ref pos, 7))); }
            else if ((b & 0x40) != 0)
            {
                int ni = ReadInt(data, ref pos, 6);
                string name = ni == 0 ? ReadStr(data, ref pos) : Lookup(ni).Name;
                string val  = ReadStr(data, ref pos);
                AddDynEntry(name, val); result.Add((name, val));
            }
            else if ((b & 0x20) != 0) { _dynMaxSize = ReadInt(data, ref pos, 5); EvictToSize(_dynMaxSize); }
            else
            {
                int ni = ReadInt(data, ref pos, 4);
                string name = ni == 0 ? ReadStr(data, ref pos) : Lookup(ni).Name;
                result.Add((name, ReadStr(data, ref pos)));
            }
        }
        return result;
    }

    private (string Name, string Value) Lookup(int idx)
    {
        if (idx <= 0)
            throw new HttpRequestException($"HPACK decode error: invalid index {idx}");
        if (idx < StaticTable.Length) return StaticTable[idx];
        int di = idx - StaticTable.Length;
        if (di >= _dyn.Count)
            throw new HttpRequestException($"HPACK decode error: index {idx} exceeds dynamic table ({_dyn.Count} entries)");
        return _dyn.ElementAt(di);
    }

    private static int ReadInt(byte[] d, ref int pos, int bits)
    {
        int mask = (1 << bits) - 1, v = d[pos++] & mask;
        if (v < mask) return v;
        int shift = 0;
        while (pos < d.Length)
        {
            byte b = d[pos++];
            if (shift >= 28) throw new HttpRequestException("HPACK integer overflow");
            v += (b & 0x7F) << shift;
            shift += 7;
            if ((b & 0x80) == 0) break;
        }
        return v;
    }

    private static string ReadStr(byte[] d, ref int pos)
    {
        if (pos >= d.Length) return "";
        bool huffman = (d[pos] & 0x80) != 0;
        int len = ReadInt(d, ref pos, 7);
        if (pos + len > d.Length)
            throw new HttpRequestException($"HPACK string truncated: need {len} bytes at offset {pos}, only {d.Length - pos} available");
        string result = huffman
            ? HuffmanDecode(d, pos, len)              // reads directly from d[pos..pos+len], no copy
            : Encoding.Latin1.GetString(d, pos, len); // also reads directly, no copy
        pos += len;
        return result;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Main HttpMessageHandler — BouncyCastle TLS + HTTP/2 when negotiated
// ─────────────────────────────────────────────────────────────────────────────
public sealed class BouncyCastleTlsHandler : HttpMessageHandler
{
    private readonly CurlImpersonateBrowserProfile _profile;
    private readonly string _profileName; // cached to avoid repeated Enum.ToString() allocations
    private readonly int[] _cipherSuites;
    private readonly bool _ignoreCert;
    private readonly bool _autoRedirect;
    private readonly int _maxRedirects;
    private readonly bool _acceptEncoding;
    private readonly IWebProxy _proxy;
    private readonly int _timeoutMs;
    // 30-second DNS cache shared across all requests from this handler
    private static readonly ConcurrentDictionary<string, (IPAddress Ip, long Expiry)> _dnsCache = new();
    // HTTP/1.1 connection pool: idle streams keyed by host:port:tls:profile → skips DNS+TCP+TLS on reuse
    private static readonly ConcurrentDictionary<string, ConcurrentQueue<(Stream S, long Exp)>> _streamPool = new();
    // HTTP/2 connection pool: reuses Http2Client (TLS + H2 handshake + HPACK state) across requests
    private static readonly ConcurrentDictionary<string, ConcurrentQueue<(Http2Client C, long Exp)>> _h2Pool = new();
    // H1.1 header ordering — Chrome fingerprint: fixed priority slots + managed set, never vary per-request.
    private static readonly string[] _h1Priority =
        { "User-Agent", "Accept", "Accept-Encoding", "Accept-Language", "Cookie" };
    private static readonly HashSet<string> _h1Managed = new(StringComparer.OrdinalIgnoreCase)
        { "Host", "Content-Length", "Connection", "Transfer-Encoding", "Accept-Encoding" };
    private static readonly HashSet<string> _h1Written =
        new(((IEnumerable<string>)_h1Priority).Concat(_h1Managed), StringComparer.OrdinalIgnoreCase);

    public BouncyCastleTlsHandler(int[] cipherSuites, bool ignoreCert, bool autoRedirect,
        int maxRedirects, bool acceptEncoding, IWebProxy proxy, int timeoutMs,
        CurlImpersonateBrowserProfile profile = CurlImpersonateBrowserProfile.Chrome142)
    {
        _cipherSuites = cipherSuites;
        _ignoreCert = ignoreCert;
        _autoRedirect = autoRedirect;
        _maxRedirects = maxRedirects;
        _acceptEncoding = acceptEncoding;
        _proxy = proxy;
        _timeoutMs = timeoutMs;
        _profile = profile;
        _profileName = profile.ToString();
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        int maxRedir = _autoRedirect ? _maxRedirects : 0;
        var method = request.Method;
        var uri = request.RequestUri;
        byte[] body = request.Content != null
            ? await request.Content.ReadAsByteArrayAsync(cancellationToken)
            : null;
        var headers = CollectHeaders(request);
        // Accumulate Set-Cookie headers from all intermediate redirects so they reach data.Cookies.
        // MergeSetCookies only forwards them to the next hop's Cookie header; without this list,
        // cookies set by intermediate responses are never written back to the global jar.
        var allIntermediateCookies = new List<string>();

        // Rotating/residential proxies assign a new exit IP per TCP session.
        // Pooling TCP streams to the proxy reuses the same session → same exit IP.
        // Disable connection pooling entirely when a proxy is configured.
        bool usePool = _proxy == null;
        bool skipPool = false;
        for (int attempt = 0; attempt <= maxRedir; attempt++)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (_timeoutMs > 0) cts.CancelAfter(_timeoutMs); // 0 means infinite; CancelAfter(0) would cancel immediately

            string pkey = PoolKey(uri);
            // Check pools in priority order: H2 (most expensive to create) → H11 stream → fresh
            Http2Client h2c = null;
            bool fromPool = false;
            bool fromH2Pool = false;
            Stream tlsStream = null;
            bool h2 = false;
            if (usePool && !skipPool && TryDequeueH2Client(pkey, out h2c))
                { h2 = true; fromH2Pool = true; }
            else if (usePool && !skipPool && TryDequeuePooled(pkey, out var pooledS))
                { fromPool = true; (tlsStream, h2) = (pooledS, false); }
            else
                (tlsStream, h2) = await ConnectAsync(uri, cts.Token);
            skipPool = false;

            try
            {
                HttpResponseMessage response;
                if (h2)
                {
                    H2Mode h2mode = _profileName.StartsWith("Firefox", StringComparison.OrdinalIgnoreCase) ? H2Mode.Firefox
                                  : _profileName.StartsWith("Safari",  StringComparison.OrdinalIgnoreCase) ? H2Mode.Safari
                                  : _profileName.StartsWith("Okhttp",  StringComparison.OrdinalIgnoreCase) ? H2Mode.OkHttp
                                  : _profileName.StartsWith("Tor",     StringComparison.OrdinalIgnoreCase) ? H2Mode.Firefox
                                  : H2Mode.Chrome;
                    // Chrome/Firefox 126+ advertise zstd; Safari, OkHttp and Tor do not
                    if (_acceptEncoding)
                        headers["Accept-Encoding"] = h2mode == H2Mode.Safari ? "gzip, deflate, br"
                            : h2mode == H2Mode.OkHttp                        ? "gzip"
                            : "gzip, deflate, br, zstd";
                    if (h2c == null) // new H2 connection from TLS negotiation
                    {
                        h2c = new Http2Client(tlsStream, h2mode);
                        await h2c.HandshakeAsync(cts.Token);
                        tlsStream = null; // h2c owns the underlying stream
                    }
                    var h2req = BuildRequest(request, uri, method, body, headers);
                    response = await h2c.SendRequestAsync(h2req, body, cts.Token);
                }
                else
                {
                    await SendHttp11Async(tlsStream, method, uri, headers, body, cts.Token);
                    response = await ReadHttp11Async(tlsStream, request, uri, cts.Token);
                }

                int status = (int)response.StatusCode;
                // Use raw Location string to avoid UriFormatException from the typed property getter
                bool hasLocation = response.Headers.TryGetValues("Location", out var rawLocVals);
                string rawLocStr = hasLocation ? (rawLocVals?.FirstOrDefault()) : null;
                Uri parsedLoc = null;
                if (rawLocStr != null)
                    Uri.TryCreate(rawLocStr, UriKind.RelativeOrAbsolute, out parsedLoc);
                if (_autoRedirect && attempt < maxRedir && IsRedirect(status) && parsedLoc != null)
                {
                    // Collect intermediate Set-Cookie before discarding this response
                    if (response.Headers.TryGetValues("Set-Cookie", out var midCookies))
                        allIntermediateCookies.AddRange(midCookies);
                    // Merge Set-Cookie from redirect response into the next request's Cookie header
                    MergeSetCookies(response, headers);
                    string prevHost = uri.Host;
                    var loc = parsedLoc;
                    if (!loc.IsAbsoluteUri)
                    {
                        if (!Uri.TryCreate(uri, loc, out loc))
                            loc = null;
                    }
                    if (loc == null) goto skipRedirect;
                    uri = loc;
                    // 301/302/303 after POST: browsers and HttpClient both convert to GET (Post/Redirect/Get pattern).
                    // 307/308 preserve the original method and body.
                    if (status is 301 or 302 or 303)
                    {
                        method = System.Net.Http.HttpMethod.Get;
                        body = null;
                        // Chrome drops content-headers when converting POST→GET (RFC 7231 §6.4)
                        headers.Remove("Content-Type");
                        headers.Remove("Content-Encoding");
                        headers.Remove("Content-Disposition");
                    }
                    // Drop Authorization on cross-origin redirects (Chrome/Firefox behaviour)
                    if (!string.Equals(uri.Host, prevHost, StringComparison.OrdinalIgnoreCase))
                        headers.Remove("Authorization");
                    response.Dispose();
                    if (h2)
                    {
                        string newKey = PoolKey(uri);
                        // Same endpoint after redirect: return H2 client to pool instead of destroying it
                        if (usePool && newKey == pkey && h2c != null && h2c.IsUsable)
                            ReturnH2ClientToPool(pkey, h2c);
                        else
                            h2c?.Dispose();
                        h2c = null;
                    }
                    else tlsStream?.Dispose();
                    continue;
                }
                skipRedirect:;
                // Inject intermediate cookies into the final response so Request.cs adds them to data.Cookies
                foreach (var sc in allIntermediateCookies)
                    response.Headers.TryAddWithoutValidation("Set-Cookie", sc);
                // Return connection to pool only when not using a proxy (proxy = new TCP per request)
                if (h2)
                {
                    if (usePool) ReturnH2ClientToPool(pkey, h2c);
                    else h2c?.Dispose();
                }
                else
                {
                    bool serverClose = response.Headers.TryGetValues("Connection", out var cv)
                        && cv.Any(v => v.Contains("close", StringComparison.OrdinalIgnoreCase));
                    if (usePool && !serverClose) { ReturnToPool(pkey, tlsStream); tlsStream = null; }
                    else tlsStream?.Dispose();
                }
                return response;
            }
            catch (Exception ex) when (
                (fromPool || fromH2Pool) &&
                (ex is IOException || ex is EndOfStreamException ||
                 (fromH2Pool && h2c != null && !h2c.IsUsable))) // GOAWAY on pooled H2 → stale, retry
            {
                // Pooled connection (H11 or H2) was stale — retry immediately with a fresh one
                tlsStream?.Dispose();
                h2c?.Dispose(); h2c = null;
                skipPool = true;
                attempt--; // don't consume a redirect budget slot for a stale-pool retry
                continue;
            }
            catch { tlsStream?.Dispose(); h2c?.Dispose(); throw; }
        }
        throw new HttpRequestException("Too many redirects");
    }

    private static bool TryDequeuePooled(string key, out Stream stream)
    {
        stream = null;
        if (!_streamPool.TryGetValue(key, out var q)) return false;
        long now = Environment.TickCount64;
        while (q.TryDequeue(out var e))
        {
            if (e.Exp > now) { stream = e.S; return true; }
            try { e.S.Dispose(); } catch { }
        }
        // Remove the empty queue so the dictionary doesn't grow unboundedly across many hosts
        if (q.IsEmpty) _streamPool.TryRemove(key, out _);
        return false;
    }

    private static void ReturnToPool(string key, Stream stream)
    {
        var q = _streamPool.GetOrAdd(key, _ => new ConcurrentQueue<(Stream, long)>());
        if (q.Count >= 4) { stream.Dispose(); return; } // cap 4 idle per endpoint
        q.Enqueue((stream, Environment.TickCount64 + 30_000));
    }

    private static bool TryDequeueH2Client(string key, out Http2Client client)
    {
        client = null;
        if (!_h2Pool.TryGetValue(key, out var q)) return false;
        long now = Environment.TickCount64;
        while (q.TryDequeue(out var e))
        {
            if (e.Exp > now && e.C.IsUsable) { client = e.C; return true; }
            try { e.C.Dispose(); } catch { }
        }
        if (q.IsEmpty) _h2Pool.TryRemove(key, out _);
        return false;
    }

    private static void ReturnH2ClientToPool(string key, Http2Client client)
    {
        if (!client.IsUsable) { client.Dispose(); return; }
        var q = _h2Pool.GetOrAdd(key, _ => new ConcurrentQueue<(Http2Client, long)>());
        if (q.Count >= 2) { client.Dispose(); return; } // cap 2 idle H2 connections per endpoint
        q.Enqueue((client, Environment.TickCount64 + 90_000)); // 90 s TTL (H2 keep-alive is typically longer)
    }

    private string PoolKey(Uri uri)
    {
        int port = uri.IsDefaultPort ? (uri.Scheme == "https" ? 443 : 80) : uri.Port;
        string proxyPart = _proxy is WebProxy wp && wp.Address != null
            ? (wp.Credentials is NetworkCredential pc && !string.IsNullOrEmpty(pc.UserName)
                ? $"{wp.Address.Host}:{wp.Address.Port}:{pc.UserName}"
                : $"{wp.Address.Host}:{wp.Address.Port}")
            : "direct";
        return $"{uri.Host}:{port}:{uri.Scheme == "https"}:{_profileName}:{_ignoreCert}:{proxyPart}";
    }

    private static async Task<IPAddress> ResolveDnsAsync(string host, CancellationToken ct)
    {
        long now = Environment.TickCount64;
        if (_dnsCache.TryGetValue(host, out var entry) && entry.Expiry > now) return entry.Ip;
        var addrs = await Dns.GetHostAddressesAsync(host, ct);
        if (addrs.Length == 0) throw new HttpRequestException($"DNS: no addresses for {host}");
        var ip = Array.Find(addrs, a => a.AddressFamily == AddressFamily.InterNetwork) ?? addrs[0];
        // Trim expired entries when cache grows large (jobs with many distinct hosts)
        if (_dnsCache.Count > 1024)
        {
            long sweep = Environment.TickCount64;
            foreach (var k in _dnsCache.Keys.ToList())
                if (_dnsCache.TryGetValue(k, out var e) && e.Expiry < sweep)
                    _dnsCache.TryRemove(k, out _);
        }
        _dnsCache[host] = (ip, now + 30_000);
        return ip;
    }

    private async Task<(Stream stream, bool h2)> ConnectAsync(Uri uri, CancellationToken ct)
    {
        bool isHttps = uri.Scheme == "https";
        string host = uri.Host;
        int port = uri.IsDefaultPort ? (isHttps ? 443 : 80) : uri.Port;

        if (_proxy is WebProxy wp && wp.Address != null)
        {
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 30);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 5);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 3);
            await socket.ConnectAsync(wp.Address.Host, wp.Address.Port, ct);
            var ns = new NetworkStream(socket, ownsSocket: true);
            try
            {
                string scheme = wp.Address.Scheme;
                bool isSocks = scheme.StartsWith("socks", StringComparison.OrdinalIgnoreCase);
                if (isSocks)
                {
                    // SOCKS proxies require their own tunnel protocol before TLS or plain HTTP.
                    // HTTP CONNECT would fail because SOCKS servers don't speak HTTP.
                    bool isSocks5 = scheme.Equals("socks5", StringComparison.OrdinalIgnoreCase);
                    if (isSocks5)
                        await Socks5TunnelAsync(ns, host, port, wp.Credentials as NetworkCredential, ct);
                    else
                        await Socks4TunnelAsync(ns, host, port, ct); // socks4 / socks4a
                }
                else if (isHttps)
                {
                    // HTTP proxy: use CONNECT to create a tunnel for TLS.
                    // Proxy-Connection: close tells the proxy gateway not to keep the tunnel
                    // alive after the response — rotating proxies assign a fresh exit IP on each new tunnel.
                    string proxyAuth = "";
                    if (wp.Credentials is NetworkCredential cred && !string.IsNullOrEmpty(cred.UserName))
                    {
                        string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{cred.UserName}:{cred.Password}"));
                        proxyAuth = $"Proxy-Authorization: Basic {encoded}\r\n";
                    }
                    await WriteAsciiAsync(ns, $"CONNECT {host}:{port} HTTP/1.1\r\nHost: {host}:{port}\r\nProxy-Connection: close\r\n{proxyAuth}\r\n", ct);
                    // Read byte-by-byte: StreamBuffer would over-read and consume TLS ServerHello bytes
                    string resp = await ReadRawLineAsync(ns, ct);
                    if (!resp.Contains("200")) throw new HttpRequestException($"Proxy CONNECT failed: {resp}");
                    while ((await ReadRawLineAsync(ns, ct)).Length > 0) { }
                }
                // For plain HTTP through an HTTP proxy: no tunnel needed — SendHttp11Async sends absolute-form URL.
                // For SOCKS (any scheme): tunnel is established above; same path as direct connection from here.
                if (!isHttps) return (ns, false);
                return await DoTlsAsync(ns, host, ct);
            }
            catch { ns.Dispose(); throw; }
        }

        // Direct connection: resolve DNS with 30 s cache, use AF-matching socket
        var ip = await ResolveDnsAsync(host, ct);
        var directSock = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        directSock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        directSock.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 30);
        directSock.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 5);
        directSock.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 3);
        await directSock.ConnectAsync(new IPEndPoint(ip, port), ct);
        var netStream = new NetworkStream(directSock, ownsSocket: true);
        try
        {
            if (!isHttps) return (netStream, false);
            return await DoTlsAsync(netStream, host, ct);
        }
        catch { netStream.Dispose(); throw; }
    }

    // ── SOCKS tunnel helpers ──────────────────────────────────────────────────

    private static async Task Socks5TunnelAsync(Stream s, string host, int port,
        NetworkCredential cred, CancellationToken ct)
    {
        byte[] hostBytes = Encoding.UTF8.GetBytes(host);
        if (hostBytes.Length > 255) throw new HttpRequestException("SOCKS5: hostname too long");

        bool hasAuth = cred != null && !string.IsNullOrEmpty(cred.UserName);

        // Greeting: version=5, nMethods, [no-auth=0x00] or [no-auth + user/pass=0x02]
        byte[] greeting = hasAuth ? new byte[] { 0x05, 0x02, 0x00, 0x02 } : new byte[] { 0x05, 0x01, 0x00 };
        await s.WriteAsync(greeting.AsMemory(), ct);

        // Server selects method
        var sel = new byte[2];
        await ReadExactBytesAsync(s, sel, ct);
        if (sel[0] != 0x05) throw new HttpRequestException($"SOCKS5: unexpected version {sel[0]}");
        if (sel[1] == 0xFF) throw new HttpRequestException("SOCKS5: no acceptable auth method");

        if (sel[1] == 0x02) // username/password auth (RFC 1929)
        {
            byte[] user = Encoding.UTF8.GetBytes(cred.UserName ?? "");
            byte[] pass = Encoding.UTF8.GetBytes(cred.Password ?? "");
            if (user.Length > 255 || pass.Length > 255) throw new HttpRequestException("SOCKS5: credentials too long");
            var authReq = new byte[3 + user.Length + pass.Length];
            authReq[0] = 0x01; authReq[1] = (byte)user.Length;
            user.CopyTo(authReq, 2);
            authReq[2 + user.Length] = (byte)pass.Length;
            pass.CopyTo(authReq, 3 + user.Length);
            await s.WriteAsync(authReq.AsMemory(), ct);
            var authResp = new byte[2];
            await ReadExactBytesAsync(s, authResp, ct);
            if (authResp[1] != 0x00) throw new HttpRequestException("SOCKS5: authentication failed");
        }

        // Connect request: VER=5 CMD=CONNECT RSV=0 ATYP=3(domain) LEN host PORT
        var req = new byte[7 + hostBytes.Length];
        req[0] = 0x05; req[1] = 0x01; req[2] = 0x00; req[3] = 0x03;
        req[4] = (byte)hostBytes.Length;
        hostBytes.CopyTo(req, 5);
        req[5 + hostBytes.Length] = (byte)(port >> 8);
        req[6 + hostBytes.Length] = (byte)(port & 0xFF);
        await s.WriteAsync(req.AsMemory(), ct);

        // Server reply: VER REP RSV ATYP [BNDADDR] BNDPORT
        var hdr = new byte[4];
        await ReadExactBytesAsync(s, hdr, ct);
        if (hdr[0] != 0x05) throw new HttpRequestException($"SOCKS5: unexpected reply version {hdr[0]}");
        if (hdr[1] != 0x00) throw new HttpRequestException($"SOCKS5: connect failed, reply code {hdr[1]}");
        // Drain bound address/port from reply
        int skip = hdr[3] switch { 0x01 => 4, 0x04 => 16, _ => hdr[3] > 0 ? hdr[3] : 1 }; // IPv4=4, IPv6=16, domain=length
        if (hdr[3] == 0x03) { var lenBuf = new byte[1]; await ReadExactBytesAsync(s, lenBuf, ct); skip = lenBuf[0]; }
        await ReadExactBytesAsync(s, new byte[skip + 2], ct); // address + 2-byte port
    }

    private static async Task Socks4TunnelAsync(Stream s, string host, int port, CancellationToken ct)
    {
        // SOCKS4a: resolve hostname on proxy side (NULL userId, hostname after NULL-terminated userId)
        byte[] hostBytes = Encoding.ASCII.GetBytes(host);
        if (hostBytes.Length > 255) throw new HttpRequestException("SOCKS4a: hostname too long");
        // SOCKS4a marker: destination IP 0.0.0.x (x != 0) signals the proxy to resolve the hostname
        var req = new byte[9 + hostBytes.Length + 1];
        req[0] = 0x04; req[1] = 0x01;
        req[2] = (byte)(port >> 8); req[3] = (byte)(port & 0xFF);
        req[4] = 0x00; req[5] = 0x00; req[6] = 0x00; req[7] = 0x01; // IP 0.0.0.1 = SOCKS4a
        req[8] = 0x00; // empty userId (null-terminated)
        hostBytes.CopyTo(req, 9);
        req[9 + hostBytes.Length] = 0x00; // null-terminate hostname
        await s.WriteAsync(req.AsMemory(), ct);

        var reply = new byte[8];
        await ReadExactBytesAsync(s, reply, ct);
        if (reply[0] != 0x00) throw new HttpRequestException($"SOCKS4a: unexpected response version {reply[0]}");
        if (reply[1] != 0x5A) throw new HttpRequestException($"SOCKS4a: connect failed, reply code {reply[1]}");
    }

    private static async Task ReadExactBytesAsync(Stream s, byte[] buf, CancellationToken ct)
    {
        int off = 0;
        while (off < buf.Length)
        {
            int r = await s.ReadAsync(buf.AsMemory(off), ct);
            if (r == 0) throw new EndOfStreamException("SOCKS proxy closed connection unexpectedly");
            off += r;
        }
    }

    private async Task<(Stream, bool)> DoTlsAsync(Stream netStream, string host, CancellationToken ct)
    {
        DefaultTlsClient tlsClient = CreateTlsClient(host);
        var protocol = new TlsClientProtocol(netStream);
        // Task.Run's ct only cancels before the task starts. Register disposes the underlying
        // stream on cancellation, which makes BouncyCastle throw IOException and unblocks the thread.
        await using var reg = ct.Register(() => { try { netStream.Dispose(); } catch { } });
        await Task.Run(() => protocol.Connect(tlsClient));

        bool h2 = tlsClient switch
        {
            ChromeTlsClient       cc => cc.NegotiatedH2,
            FirefoxTlsClient      ff => ff.NegotiatedH2,
            SafariLegacyTlsClient sl => sl.NegotiatedH2,
            SafariiOSTlsClient    si => si.NegotiatedH2,
            SafariTlsClient       sf => sf.NegotiatedH2,
            OkhttpTlsClient       ok => ok.NegotiatedH2,
            TorTlsClient          tr => tr.NegotiatedH2,
            _ => false
        };

        return (protocol.Stream, h2);
    }

    private DefaultTlsClient CreateTlsClient(string host)
    {
        string name = _profileName;
        if (name.StartsWith("Firefox", StringComparison.OrdinalIgnoreCase))
            return new FirefoxTlsClient(_cipherSuites, _ignoreCert, host);
        if (name.StartsWith("Safari", StringComparison.OrdinalIgnoreCase))
        {
            bool isIos = name.Contains("Ios", StringComparison.OrdinalIgnoreCase)
                      || name.Contains("Ipad", StringComparison.OrdinalIgnoreCase);
            if (isIos) return new SafariiOSTlsClient(_cipherSuites, _ignoreCert, host);
            // Safari 15.x profiles: pre-17 fingerprint (no secp521r1, old extension order)
            bool isLegacy = name == "Safari153" || name == "Safari155";
            return isLegacy
                ? (DefaultTlsClient)new SafariLegacyTlsClient(_cipherSuites, _ignoreCert, host)
                : new SafariTlsClient(_cipherSuites, _ignoreCert, host);
        }
        if (name.StartsWith("Okhttp", StringComparison.OrdinalIgnoreCase))
            return new OkhttpTlsClient(_cipherSuites, _ignoreCert, host);
        if (name.StartsWith("Tor", StringComparison.OrdinalIgnoreCase))
            return new TorTlsClient(_cipherSuites, _ignoreCert, host);
        return new ChromeTlsClient(_cipherSuites, _ignoreCert, host);
    }

    private static bool IsRedirect(int s) => s is 301 or 302 or 303 or 307 or 308;

    // Merge Set-Cookie headers from a redirect response into the next request's Cookie header.
    // Without this, OAuth multi-hop redirects lose session cookies set by intermediate responses.
    private static void MergeSetCookies(HttpResponseMessage response, Dictionary<string, string> headers)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var setCookies)) return;
        var jar = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (headers.TryGetValue("Cookie", out var existing))
            foreach (var pair in existing.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = pair.Trim().Split('=', 2);
                if (kv.Length == 2) jar[kv[0].Trim()] = kv[1].Trim();
            }
        foreach (var sc in setCookies)
        {
            var parts = sc.Split(';')[0].Split('=', 2);
            if (parts.Length == 2) jar[parts[0].Trim()] = parts[1].Trim();
        }
        if (jar.Count > 0)
            headers["Cookie"] = string.Join("; ", jar.Select(kv => $"{kv.Key}={kv.Value}"));
    }

    // ── HTTP/1.1 ─────────────────────────────────────────────────────────────

    private static Dictionary<string, string> CollectHeaders(HttpRequestMessage req)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var h in req.Headers)
            d[h.Key] = h.Key.Equals("Cookie", StringComparison.OrdinalIgnoreCase)
                ? string.Join("; ", h.Value) : string.Join(", ", h.Value);
        if (req.Content != null)
            foreach (var h in req.Content.Headers)
                d[h.Key] = h.Key.Equals("Cookie", StringComparison.OrdinalIgnoreCase)
                    ? string.Join("; ", h.Value) : string.Join(", ", h.Value);
        return d;
    }

    private static HttpRequestMessage BuildRequest(HttpRequestMessage orig, Uri uri,
        System.Net.Http.HttpMethod method, byte[] body, Dictionary<string, string> headers)
    {
        var r = new HttpRequestMessage(method, uri);
        if (body != null) r.Content = new ByteArrayContent(body);
        foreach (var h in headers)
        {
            // Content-Type/Content-Encoding belong in Content.Headers, not request headers
            bool isContentHeader = h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)
                                || h.Key.Equals("Content-Encoding", StringComparison.OrdinalIgnoreCase)
                                || h.Key.Equals("Content-Disposition", StringComparison.OrdinalIgnoreCase);
            if (isContentHeader && r.Content != null)
                r.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
            else if (!h.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                r.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }
        return r;
    }

    private async Task SendHttp11Async(Stream stream, System.Net.Http.HttpMethod method,
        Uri uri, Dictionary<string, string> headers, byte[] body, CancellationToken ct)
    {
        // Plain HTTP through an HTTP proxy requires an absolute-form request-line (RFC 7230 §5.3.2).
        // Through a SOCKS proxy the tunnel already points to the target, so use origin-form like a direct connection.
        bool isHttpProxy = _proxy is WebProxy proxyWp && proxyWp.Address != null
            && !proxyWp.Address.Scheme.StartsWith("socks", StringComparison.OrdinalIgnoreCase);
        string path = (isHttpProxy && uri.Scheme == "http")
            ? uri.AbsoluteUri
            : (uri.PathAndQuery.Length > 0 ? uri.PathAndQuery : "/");
        string host = uri.Host + (uri.IsDefaultPort ? "" : ":" + uri.Port);

        bool isSafari = _profileName.StartsWith("Safari", StringComparison.OrdinalIgnoreCase);
        // With a rotating proxy, Connection: close tells both the proxy and target to drop the
        // session after each response — the proxy then assigns a fresh exit IP next connection.
        // Direct connections use keep-alive for performance.
        bool connClose = _proxy is WebProxy pwp && pwp.Address != null;
        var sb = new StringBuilder();
        sb.Append($"{method.Method} {path} HTTP/1.1\r\nHost: {host}\r\n");
        sb.Append(connClose ? "Connection: close\r\n" : "Connection: keep-alive\r\n");
        if (body != null) sb.Append($"Content-Length: {body.Length}\r\n");

        // Chrome HTTP/1.1 fingerprint order: User-Agent → Accept → Accept-Encoding → Accept-Language → Cookie → rest
        foreach (var key in _h1Priority)
        {
            if (key.Equals("Accept-Encoding", StringComparison.OrdinalIgnoreCase))
            {
                if (_acceptEncoding)
                    sb.Append(isSafari ? "Accept-Encoding: gzip, deflate, br\r\n"
                                       : "Accept-Encoding: gzip, deflate, br, zstd\r\n");
                // else: omit Accept-Encoding entirely — caller disabled it
            }
            else if (headers.TryGetValue(key, out var hv))
                sb.Append($"{key}: {hv}\r\n");
        }
        // Remaining headers in insertion order, skipping managed + priority (pre-computed static set)
        foreach (var (k, v) in headers)
            if (!_h1Written.Contains(k)) sb.Append($"{k}: {v}\r\n");
        sb.Append("\r\n");

        byte[] hdrBytes = Encoding.Latin1.GetBytes(sb.ToString());
        // Combine headers and body into one write → one TLS record. Chrome does the same;
        // two separate writes with NoDelay=true produce two TCP packets, detectable by fingerprinters.
        if (body?.Length > 0)
        {
            var combined = new byte[hdrBytes.Length + body.Length];
            hdrBytes.CopyTo(combined, 0);
            body.CopyTo(combined, hdrBytes.Length);
            await stream.WriteAsync(combined.AsMemory(), ct);
        }
        else
            await stream.WriteAsync(hdrBytes.AsMemory(), ct);
    }

    private async Task<HttpResponseMessage> ReadHttp11Async(Stream stream,
        HttpRequestMessage original, Uri currentUri, CancellationToken ct)
    {
        var buf = new StreamBuffer(stream); // 4 KB buffer — eliminates per-byte TLS decrypt calls
        string statusLine = await buf.ReadLineAsync(ct);
        var parts = statusLine.Split(' ', 3);
        int status = int.TryParse(parts.ElementAtOrDefault(1), out int sc) ? sc : 0;

        var respHeaders = new List<(string Key, string Value)>();
        while (true)
        {
            string line = await buf.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line)) break;
            int col = line.IndexOf(':');
            if (col >= 0) respHeaders.Add((line[..col].Trim(), line[(col + 1)..].Trim()));
        }

        // RFC 7230 §3.3 / RFC 7231 §6.2: 1xx are informational — discard and read the final response.
        // Returning a 100/103 to the caller would corrupt the pool: the real 200 response bytes remain
        // in the stream and would be misread as the next request's response.
        while (status is >= 100 and <= 199)
        {
            statusLine = await buf.ReadLineAsync(ct);
            parts = statusLine.Split(' ', 3);
            status = int.TryParse(parts.ElementAtOrDefault(1), out int sc2) ? sc2 : 0;
            respHeaders.Clear();
            while (true)
            {
                string l = await buf.ReadLineAsync(ct);
                if (string.IsNullOrEmpty(l)) break;
                int col = l.IndexOf(':');
                if (col >= 0) respHeaders.Add((l[..col].Trim(), l[(col + 1)..].Trim()));
            }
        }

        string te  = respHeaders.LastOrDefault(h => h.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)).Value ?? "";
        string cl  = respHeaders.LastOrDefault(h => h.Key.Equals("Content-Length",    StringComparison.OrdinalIgnoreCase)).Value ?? "";
        string ce  = respHeaders.LastOrDefault(h => h.Key.Equals("Content-Encoding",  StringComparison.OrdinalIgnoreCase)).Value ?? "";
        bool wasChunked = te.Contains("chunked", StringComparison.OrdinalIgnoreCase);

        byte[] raw;
        // RFC 7230 §3.3: HEAD responses, 204, and 304 never have a body even if Content-Length is present.
        // Without this guard, ReadExactAsync would block indefinitely waiting for bytes that never arrive.
        // RFC 7231 §6.3.6: 205 Reset Content must also have no body.
        bool noBody = status is 204 or 205 or 304 || original.Method == System.Net.Http.HttpMethod.Head;
        if (noBody)
            raw = Array.Empty<byte>();
        else if (te.Contains("chunked", StringComparison.OrdinalIgnoreCase))
            raw = await buf.ReadChunkedAsync(ct);
        else if (int.TryParse(cl, out int clen) && clen >= 0)
            raw = await buf.ReadExactAsync(clen, ct);
        else
            raw = await buf.ReadUntilCloseAsync(ct);

        byte[] body = await TlsHelpers.DecompressAsync(raw, ce);

        // Use currentUri so response.RequestMessage.RequestUri reflects the final URL after
        // any redirects — without this, data.Address always shows the original request URL.
        var reqMsg = currentUri == original.RequestUri
            ? original
            : new HttpRequestMessage(original.Method, currentUri);

        var response = new HttpResponseMessage((HttpStatusCode)status)
        {
            RequestMessage = reqMsg,
            Content = new ByteArrayContent(body)
        };
        foreach (var (k, v) in respHeaders)
        {
            // Strip transfer-encoding headers after decoding; ByteArrayContent sets correct Content-Length.
            if (k.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)) continue; // body already decoded
            if (ce.Length > 0 && k.Equals("Content-Encoding", StringComparison.OrdinalIgnoreCase)) continue;
            if ((ce.Length > 0 || wasChunked) && k.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) continue;
            if (!response.Headers.TryAddWithoutValidation(k, v))
                response.Content.Headers.TryAddWithoutValidation(k, v);
        }
        return response;
    }

    // ── I/O helpers ──────────────────────────────────────────────────────────

    private static async Task WriteAsciiAsync(Stream s, string text, CancellationToken ct)
    {
        byte[] b = Encoding.ASCII.GetBytes(text);
        await s.WriteAsync(b, 0, b.Length, ct);
    }

    // Reads one HTTP header line byte-by-byte from a raw socket stream.
    // Unlike StreamBuffer, it never over-reads, so TLS bytes that immediately
    // follow the CONNECT 200 response are left in the socket for BouncyCastle.
    private static async Task<string> ReadRawLineAsync(Stream s, CancellationToken ct)
    {
        var sb = new StringBuilder(128);
        var buf = new byte[1];
        while (true)
        {
            int n = await s.ReadAsync(buf, 0, 1, ct);
            if (n == 0) break;
            if (buf[0] == '\n') return sb.ToString();
            if (buf[0] != '\r') sb.Append((char)buf[0]);
        }
        return sb.ToString();
    }

    // Buffered stream reader — eliminates the per-byte TLS decrypt overhead of the old
    // 1-byte ReadAsync loop. Each Fill() call decrypts one TLS record (~16 KB) at once.
    private sealed class StreamBuffer
    {
        private readonly Stream _inner;
        private readonly byte[] _buf = new byte[4096];
        private int _pos, _len;

        public StreamBuffer(Stream inner) => _inner = inner;

        private async ValueTask<bool> FillAsync(CancellationToken ct)
        {
            _pos = 0;
            _len = await _inner.ReadAsync(_buf.AsMemory(), ct);
            return _len > 0;
        }

        public async Task<string> ReadLineAsync(CancellationToken ct)
        {
            var sb = new StringBuilder(128);
            while (true)
            {
                if (_pos >= _len && !await FillAsync(ct)) break;
                while (_pos < _len)
                {
                    byte b = _buf[_pos++];
                    if (b == '\n') return sb.ToString();
                    if (b != '\r') sb.Append((char)b);
                }
            }
            return sb.ToString();
        }

        public async Task<byte[]> ReadExactAsync(int count, CancellationToken ct)
        {
            if (count == 0) return Array.Empty<byte>();
            var result = new byte[count];
            int off = 0;
            if (_pos < _len)
            {
                int take = Math.Min(_len - _pos, count);
                Buffer.BlockCopy(_buf, _pos, result, 0, take);
                _pos += take; off += take;
            }
            while (off < count)
            {
                int r = await _inner.ReadAsync(result.AsMemory(off, count - off), ct);
                if (r == 0) throw new EndOfStreamException($"Connection closed after {off}/{count} bytes");
                off += r;
            }
            return result;
        }

        // Reads exactly count bytes directly into target without an intermediate allocation.
        private async Task ReadExactIntoAsync(MemoryStream target, int count, CancellationToken ct)
        {
            if (_pos < _len) // drain the internal StreamBuffer first
            {
                int take = Math.Min(_len - _pos, count);
                target.Write(_buf, _pos, take);
                _pos += take; count -= take;
            }
            while (count > 0) // read remainder directly from the underlying stream using _buf as relay
            {
                int toRead = Math.Min(_buf.Length, count);
                int r = await _inner.ReadAsync(_buf.AsMemory(0, toRead), ct);
                if (r == 0) throw new EndOfStreamException($"Connection closed reading chunk body ({count} bytes remaining)");
                target.Write(_buf, 0, r);
                count -= r;
            }
        }

        public async Task<byte[]> ReadChunkedAsync(CancellationToken ct)
        {
            using var ms = new MemoryStream();
            while (true)
            {
                string sizeLine = await ReadLineAsync(ct);
                int semi = sizeLine.IndexOf(';');
                if (semi >= 0) sizeLine = sizeLine[..semi];
                if (!int.TryParse(sizeLine.Trim(), System.Globalization.NumberStyles.HexNumber, null, out int chunkSize))
                    throw new HttpRequestException($"Invalid chunk size: '{sizeLine}'");
                if (chunkSize == 0) { while ((await ReadLineAsync(ct)).Length > 0) { } break; } // drain optional trailers
                await ReadExactIntoAsync(ms, chunkSize, ct); // no intermediate byte[] allocation
                await ReadLineAsync(ct); // trailing CRLF after chunk data
            }
            return ms.ToArray();
        }

        public async Task<byte[]> ReadUntilCloseAsync(CancellationToken ct)
        {
            var ms = new MemoryStream();
            if (_pos < _len) ms.Write(_buf, _pos, _len - _pos); // drain buffered bytes
            _pos = _len = 0;
            byte[] tmp = ArrayPool<byte>.Shared.Rent(8192);
            try
            {
                int r;
                while ((r = await _inner.ReadAsync(tmp.AsMemory(), ct)) > 0) ms.Write(tmp, 0, r);
            }
            finally { ArrayPool<byte>.Shared.Return(tmp); }
            return ms.ToArray();
        }
    } // end StreamBuffer
}
