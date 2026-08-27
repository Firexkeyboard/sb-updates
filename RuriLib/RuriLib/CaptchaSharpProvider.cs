using System.Threading.Tasks;
using CaptchaSharp;
using CloudflareSolverRe.CaptchaProviders;
using CloudflareSolverRe.Types.Captcha;

namespace RuriLib;

internal class CaptchaSharpProvider : ICaptchaProvider
{
    private readonly CaptchaService _service;

    public string Name => _service?.GetType().Name ?? "None";

    public CaptchaSharpProvider(CaptchaService service)
    {
        _service = service;
    }

    public async Task<CaptchaSolveResult> SolveCaptcha(string siteKey, string siteUrl)
    {
        if (_service == null)
            return new CaptchaSolveResult { Success = false };
        try
        {
            var solution = await _service.SolveRecaptchaV2Async(siteKey, siteUrl, string.Empty);
            return new CaptchaSolveResult { Success = true, Response = solution.Response };
        }
        catch
        {
            return new CaptchaSolveResult { Success = false };
        }
    }
}
