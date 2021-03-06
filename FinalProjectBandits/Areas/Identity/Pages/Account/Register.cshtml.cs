﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using FinalProjectBandits.Data;
using FinalProjectBandits.Models;
using FinalProjectBandits.Services;
using Google.GData.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace FinalProjectBandits.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _dbContext;
        private readonly ICombinedAPIService _combinedAPIService;
        private int _objectID = 0;
        private FeatureSet _featureSet = new FeatureSet();
        private bool _isInPolygon = false;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            ApplicationDbContext dbContext,
            ICombinedAPIService combinedAPIService
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _dbContext = dbContext;
            _combinedAPIService = combinedAPIService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        //public FeatureSet FeatureSet { get; set; }

        public class InputModel
        {
            [Required]
            //[DataType(DataType.Text)]
            [Display(Name = "First Name")]
            public string First_Name { get; set; }

            [Required]
            //[DataType(DataType.Text)]
            [Display(Name = "Last Name")]
            public string Last_Name { get; set; }

            [Required]
            //[DataType(DataType.Text)]
            [Display(Name = "Street")]
            public string Street { get; set; }

            [Required]
            //[DataType(DataType.Text)]
            [Display(Name = "City")]
            public string City { get; set; }

            [Required]
            //[DataType(DataType.Text)]
            [Display(Name = "State")]
            public string State { get; set; }

            [Required]
            //[DataType(DataType.Text)]
            [Display(Name = "Zip")]
            public string Zip { get; set; }

            [Required]
            //[DataType(DataType.Text)]
            [Display(Name = "Phone")]
            public string Phone { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = Input.Email, Email = Input.Email };
                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");
                    var customer = new Customer { 
                        First_Name = Input.First_Name, 
                        Last_Name = Input.Last_Name,
                        Street = Input.Street,
                        City = Input.City,
                        State = Input.State,
                        Zip = Input.Zip,
                        Phone = Input.Phone,
                        Email = Input.Email, 
                        UserId = user.Id 
                    };

                    // API in here?
                    var apiObject = await _combinedAPIService.GetCombinedObject($"{customer.Street} {customer.City}, " +
                        $"{customer.State} {customer.Zip}");

                    var newCustomerAddress = new CoordinatePoint
                    {
                        Latitude = apiObject.GeocodingObject.Results[0].Geometry.Location.Lat,
                        Longitude = apiObject.GeocodingObject.Results[0].Geometry.Location.Lng,
                    };

                    foreach (FeatureSet featureSet in apiObject.MUAPObject.Features)
                    {
  
                        var isInPolygon = _combinedAPIService.IsPointInPolygon(newCustomerAddress, featureSet.Geometry.Polygon.Coordinates);
                        if (isInPolygon == true)
                        {
                            customer.MuapIndex = featureSet.Attributes.Muap_index;
                            break;

                        }
                    }
                    

                    //this is what's added to connect to the customer db
                    _dbContext.Customers.Add(customer);
                    await _dbContext.SaveChangesAsync();


                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { 
                            area = "Identity", 
                            userId = user.Id, 
                            code = code, 
                            returnUrl = returnUrl 
                        },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { 
                            email = Input.Email, 
                            returnUrl = returnUrl 
                        });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }


            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
