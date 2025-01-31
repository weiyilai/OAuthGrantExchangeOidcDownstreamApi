﻿using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Web;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EntraIdRazorPage.Pages;

[AuthorizeForScopes(Scopes = new string[] { "api://b2a09168-54e2-4bc4-af92-a710a64ef1fa/access_as_user" })]
public class CallApiModel : PageModel
{
    private readonly EntraIdApiService _apiService;

    public List<string>? DataFromApi { get; set; }

    public CallApiModel(EntraIdApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task OnGetAsync()
    {
        DataFromApi = await _apiService.GetApiDataAsync();
    }
}