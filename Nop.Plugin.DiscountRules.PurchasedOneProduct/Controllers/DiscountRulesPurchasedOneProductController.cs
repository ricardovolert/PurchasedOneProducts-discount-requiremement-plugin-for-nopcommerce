﻿using System;
using System.Linq;
using System.Web.Mvc;
using Nop.Core.Domain.Discounts;
using Nop.Plugin.DiscountRules.PurchasedOneProduct.Models;
using Nop.Services.Configuration;
using Nop.Services.Discounts;
using Nop.Services.Security;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Security;

namespace Nop.Plugin.DiscountRules.PurchasedOneProduct.Controllers
{
    [AdminAuthorize]
    public class DiscountRulesPurchasedOneProductController : BasePluginController
    {
        #region Fields

        private readonly IDiscountService _discountService;
        private readonly ISettingService _settingService;
        private readonly IPermissionService _permissionService;

        #endregion

        #region Ctor

        public DiscountRulesPurchasedOneProductController(IDiscountService discountService,
            ISettingService settingService, IPermissionService permissionService)
        {
            this._discountService = discountService;
            this._settingService = settingService;
            this._permissionService = permissionService;
        }

        #endregion

        #region Methods

        public ActionResult Configure(int discountId, int? discountRequirementId)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDiscounts))
                return Content("Access denied");

            var discount = _discountService.GetDiscountById(discountId);

            if (discount == null)
                throw new ArgumentException("Discount could not be loaded");

            if (discountRequirementId.HasValue)
            {
                var discountRequirement = discount.DiscountRequirements.FirstOrDefault(dr => dr.Id == discountRequirementId.Value);

                if (discountRequirement == null)
                    return Content("Failed to load requirement.");
            }

            var restrictedProductVariantIds = _settingService.GetSettingByKey<string>(string.Format("DiscountRequirement.RestrictedProductVariantIds-{0}", discountRequirementId.HasValue ? discountRequirementId.Value : 0));

            var model = new RequirementModel
            {
                RequirementId = discountRequirementId.HasValue ? discountRequirementId.Value : 0,
                DiscountId = discountId,
                ProductVariants = restrictedProductVariantIds
            };

            //add a prefix
            ViewData.TemplateInfo.HtmlFieldPrefix = string.Format("DiscountRulesPurchasedOneProduct{0}", discountRequirementId.HasValue ? discountRequirementId.Value.ToString() : "0");

            return View("~/Plugins/DiscountRules.PurchasedOneProduct/Views/DiscountRulesPurchasedOneProduct/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult Configure(int discountId, int? discountRequirementId, string variantIds)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDiscounts))
                return Content("Access denied");

            var discount = _discountService.GetDiscountById(discountId);

            if (discount == null)
                throw new ArgumentException("Discount could not be loaded");

            DiscountRequirement discountRequirement = null;

            if (discountRequirementId.HasValue)
                discountRequirement = discount.DiscountRequirements.FirstOrDefault(dr => dr.Id == discountRequirementId.Value);

            if (discountRequirement != null)
            {
                //update existing rule
                _settingService.SetSetting(string.Format("DiscountRequirement.RestrictedProductVariantIds-{0}", discountRequirement.Id), variantIds);
            }
            else
            {
                //save new rule
                discountRequirement = new DiscountRequirement()
                {
                    DiscountRequirementRuleSystemName = "DiscountRequirement.PurchasedOneProduct"
                };

                discount.DiscountRequirements.Add(discountRequirement);
                _discountService.UpdateDiscount(discount);
                
                _settingService.SetSetting(string.Format("DiscountRequirement.RestrictedProductVariantIds-{0}", discountRequirement.Id), variantIds);
            }

            return Json(new { Result = true, NewRequirementId = discountRequirement.Id }, JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}