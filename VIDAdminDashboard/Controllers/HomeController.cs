using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using VIDAdminDashboard.Models;
using VIDAdminDashboard.Services;

namespace VIDAdminDashboard.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IVerifiedIdService _verifiedIdService;
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly ILogger<HomeController> _logger;
    private const string Scope = "6a8b4b39-c021-437c-b060-5a14a3fd65f3/full_access";
    private static readonly string[] Scopes = new[] { Scope };

    public HomeController(
        IVerifiedIdService verifiedIdService,
        ITokenAcquisition tokenAcquisition,
        ILogger<HomeController> logger)
    {
        _verifiedIdService = verifiedIdService;
        _tokenAcquisition = tokenAcquisition;
        _logger = logger;
    }

    [AuthorizeForScopes(Scopes = new[] { "6a8b4b39-c021-437c-b060-5a14a3fd65f3/full_access" })]
    public async Task<IActionResult> Index(string? authorityId = null)
    {
        var viewModel = new DashboardViewModel();

        try
        {
            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(Scopes);
            
            // Get all authorities
            viewModel.Authorities = await _verifiedIdService.GetAuthoritiesAsync(accessToken);
            
            // If an authority is selected, get its details and contracts
            if (!string.IsNullOrEmpty(authorityId))
            {
                viewModel.SelectedAuthority = viewModel.Authorities.FirstOrDefault(a => a.Id == authorityId);
                if (viewModel.SelectedAuthority != null)
                {
                    viewModel.Contracts = await _verifiedIdService.GetContractsAsync(accessToken, authorityId);
                }
            }
            else if (viewModel.Authorities.Count > 0)
            {
                // Select the first authority by default
                viewModel.SelectedAuthority = viewModel.Authorities.First();
                viewModel.Contracts = await _verifiedIdService.GetContractsAsync(accessToken, viewModel.SelectedAuthority.Id);
            }
        }
        catch (MicrosoftIdentityWebChallengeUserException ex)
        {
            _logger.LogWarning(ex, "User needs to consent or re-authenticate");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard");
            viewModel.ErrorMessage = "Failed to load dashboard data. Please ensure you have the required permissions.";
        }

        return View(viewModel);
    }

    [HttpPost]
    public IActionResult SelectAuthority(string authorityId)
    {
        return RedirectToAction(nameof(Index), new { authorityId });
    }

    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
    }
}
