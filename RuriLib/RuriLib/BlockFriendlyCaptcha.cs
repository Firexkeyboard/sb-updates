using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto.Digests;
using RuriLib.LS;
using RuriLib.Models;

namespace RuriLib;

public class BlockFriendlyCaptcha : BlockBase
{
    private string siteKey       = "";
    private bool   useEuEndpoint = false;
    private string outputVariable = "FRCAPTCHA_TOKEN";

    public string SiteKey
    {
        get => siteKey;
        set { siteKey = value; OnPropertyChanged("SiteKey"); }
    }
    public bool UseEuEndpoint
    {
        get => useEuEndpoint;
        set { useEuEndpoint = value; OnPropertyChanged("UseEuEndpoint"); }
    }
    public string OutputVariable
    {
        get => outputVariable;
        set { outputVariable = value; OnPropertyChanged("OutputVariable"); }
    }

    public BlockFriendlyCaptcha() { Label = "FRIENDLY CAPTCHA"; }

    public override BlockBase FromLS(string line)
    {
        string input = line.Trim();
        if (input.StartsWith("#")) Label = LineParser.ParseLabel(ref input);
        SiteKey       = LineParser.ParseLiteral(ref input, "SiteKey");
        if (input != "" && LineParser.Lookahead(ref input) == TokenType.Boolean)
            UseEuEndpoint = LineParser.ParseToken(ref input, TokenType.Boolean, false).Equals("TRUE", StringComparison.OrdinalIgnoreCase);
        if (input != "" && LineParser.Lookahead(ref input) == TokenType.Literal)
            OutputVariable = LineParser.ParseLiteral(ref input, "OutputVariable");
        return this;
    }

    public override string ToLS(bool indent = true)
    {
        var bw = new BlockWriter(GetType(), indent, Disabled);
        bw.Label(Label).Token("FRIENDLYCAPTCHA").Literal(SiteKey).Boolean(UseEuEndpoint, "UseEuEndpoint").Literal(OutputVariable);
        return bw.ToString();
    }

    public override void Process(BotData data)
    {
        base.Process(data);

        string key    = ReplaceValues(siteKey, data);
        string outVar = ReplaceValues(outputVariable, data);

        string apiUrl = useEuEndpoint
            ? "https://eu-api.friendlycaptcha.eu/api/v1/puzzle"
            : "https://api.friendlycaptcha.com/api/v1/puzzle";

        data.Log(new LogEntry("FriendlyCaptcha Solver", Colors.GreenYellow));
        data.Log(new LogEntry($"Site Key      : {key}", Colors.GreenYellow));
        data.Log(new LogEntry($"Endpoint      : {(useEuEndpoint ? "EU" : "Global")}", Colors.GreenYellow));

        // ── 1. Fetch puzzle ───────────────────────────────────────────────────
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",    "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        http.DefaultRequestHeaders.TryAddWithoutValidation("Accept",        "application/json");
        http.DefaultRequestHeaders.TryAddWithoutValidation("x-frc-client",  "js-0.9.20");

        string puzzleFull;
        try
        {
            string url  = $"{apiUrl}?sitekey={Uri.EscapeDataString(key)}";
            string body = http.GetStringAsync(url).GetAwaiter().GetResult();
            var    root = JObject.Parse(body);
            var    data2 = root["data"];
            puzzleFull = (data2 != null ? data2["puzzle"] : root["puzzle"])?.ToString()
                ?? throw new Exception("Puzzle field not found in API response");
        }
        catch (Exception ex)
        {
            data.Log(new LogEntry($"Status        : Fetch Error", Colors.Tomato));
            data.Log(new LogEntry($"Response      : {ex.Message}", Colors.Tomato));
            data.Log(new LogEntry($"{{\"solution\":null,\"status\":\"error\",\"errorId\":1,\"errorCode\":\"ERROR_FETCH\",\"errorDescription\":\"{ex.Message.Replace("\"","'")}\" }}", Colors.Tomato));
            throw new Exception($"[FRC] Fetch error: {ex.Message}");
        }

        // ── 2. Decode puzzle ─────────────────────────────────────────────────
        // Format: "SIGNATURE.BASE64DATA"
        var parts = puzzleFull.Split('.', 2);
        if (parts.Length < 2) throw new Exception("[FRC] Unexpected puzzle format");
        string sig      = parts[0];
        string b64Part  = parts[1];

        byte[] rawBytes = Convert.FromBase64String(b64Part.PadRight(b64Part.Length + (4 - b64Part.Length % 4) % 4, '='));

        const int PuzzleSize            = 128;
        const int NumPuzzlesOffset      = 14;
        const int DifficultyOffset      = 15;

        // Pad to 128 bytes
        byte[] puzzleBytes = new byte[PuzzleSize];
        Array.Copy(rawBytes, puzzleBytes, Math.Min(rawBytes.Length, PuzzleSize));

        int  nPuzzles  = puzzleBytes[NumPuzzlesOffset];
        int  difficulty = puzzleBytes[DifficultyOffset];
        uint threshold  = DifficultyToThreshold(difficulty);

        data.Log(new LogEntry($"Puzzles       : {nPuzzles}  |  Difficulty: {difficulty}  |  Threshold: {threshold:N0}", Colors.GreenYellow));

        // ── 3. Solve sub-puzzles (parallel) ───────────────────────────────────
        var sw        = System.Diagnostics.Stopwatch.StartNew();
        var solutions = new byte[nPuzzles][];

        Parallel.For(0, nPuzzles, i =>
        {
            solutions[i] = SolvePuzzle(puzzleBytes, threshold, i);
        });

        sw.Stop();
        long elapsed = sw.ElapsedMilliseconds;

        // ── 4. Build token ────────────────────────────────────────────────────
        byte[] combined = solutions.SelectMany(s => s).ToArray();

        byte[] diag = new byte[3];
        diag[0] = 1;
        ushort elapsedMs = (ushort)Math.Min(elapsed, 65535);
        diag[1] = (byte)(elapsedMs >> 8);
        diag[2] = (byte)(elapsedMs & 0xFF);

        string token = $"{sig}.{b64Part}.{Convert.ToBase64String(combined)}.{Convert.ToBase64String(diag)}";

        data.Variables.Set(new CVar(outVar, token));

        data.Log(new LogEntry($"Elapsed       : {elapsed} ms", Colors.GreenYellow));
        string result = $"{{\"solution\":{{\"frcaptchaResponse\":\"{token}\"}},\"status\":\"ready\",\"errorId\":0,\"errorCode\":null,\"errorDescription\":null}}";
        data.Log(new LogEntry(result, Colors.GreenYellow));
        data.ResponseSource = result;
        data.Log(new LogEntry($"Saved to <{outVar}>", Colors.Lime));
    }

    // ── BLAKE2b PoW solver for one sub-puzzle ─────────────────────────────────
    private static byte[] SolvePuzzle(byte[] puzzleTemplate, uint threshold, int puzzleIdx)
    {
        byte[] buf    = new byte[128];
        Array.Copy(puzzleTemplate, buf, puzzleTemplate.Length);
        buf[120] = (byte)(puzzleIdx & 0xFF);

        var  digest  = new Blake2bDigest(256);  // 32-byte output
        byte[] hash  = new byte[32];

        for (int b = 0; b < 256; b++)
        {
            buf[123] = (byte)b;
            for (uint nonce = 0; nonce < uint.MaxValue; nonce++)
            {
                buf[124] = (byte)(nonce & 0xFF);
                buf[125] = (byte)((nonce >> 8)  & 0xFF);
                buf[126] = (byte)((nonce >> 16) & 0xFF);
                buf[127] = (byte)((nonce >> 24) & 0xFF);

                digest.BlockUpdate(buf, 0, 128);
                digest.DoFinal(hash, 0);

                // Compare first 4 bytes as little-endian uint32
                uint val = (uint)(hash[0] | (hash[1] << 8) | (hash[2] << 16) | (hash[3] << 24));
                if (val < threshold)
                    return buf[120..128];
            }
        }

        throw new Exception($"[FRC] No solution found for sub-puzzle {puzzleIdx}");
    }

    // threshold = 2^((255.999 - difficulty) / 8)  (clamped to uint32)
    private static uint DifficultyToThreshold(int difficulty)
    {
        difficulty = Math.Max(0, Math.Min(255, difficulty));
        double v = Math.Pow(2.0, (255.999 - difficulty) / 8.0);
        return v >= uint.MaxValue ? uint.MaxValue : (uint)v;
    }
}
