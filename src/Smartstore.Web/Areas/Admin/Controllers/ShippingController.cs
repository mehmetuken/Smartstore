﻿using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Security;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Shipping;
using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class ShippingController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IShippingService _shippingService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IProviderManager _providerManager;
        private readonly ModuleManager _moduleManager;
        private readonly IWidgetService _widgetService;
        private readonly IRuleService _ruleService;
        private readonly ShippingSettings _shippingSettings;

        public ShippingController(
            SmartDbContext db,
            IShippingService shippingService,
            ILocalizedEntityService localizedEntityService,
            IStoreMappingService storeMappingService,
            IProviderManager providerManager,
            ModuleManager moduleManager,
            IWidgetService widgetService,
            IRuleService ruleService,
            ShippingSettings shippingSettings)
        {
            _db = db;
            _shippingService = shippingService;
            _localizedEntityService = localizedEntityService;
            _storeMappingService = storeMappingService;
            _providerManager = providerManager;
            _moduleManager = moduleManager;
            _widgetService = widgetService;
            _ruleService = ruleService;
            _shippingSettings = shippingSettings;
        }

        #region Utilities

        private async Task UpdateLocalesAsync(ShippingMethod shippingMethod, ShippingMethodModel model)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(shippingMethod, x => x.Name, localized.Name, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(shippingMethod, x => x.Description, localized.Description, localized.LanguageId);
            }
        }

        private async Task PrepareShippingMethodModelAsync(ShippingMethodModel model, ShippingMethod shippingMethod)
        {
            if (shippingMethod != null)
            {
                model.SelectedRuleSetIds = shippingMethod.RuleSets.Select(x => x.Id).ToArray();
            }

            model.SelectedStoreIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(shippingMethod);
        }

        #endregion

        #region Providers (Shipping rate computation methods)

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Providers));
        }

        [Permission(Permissions.Configuration.Tax.Read)]
        public IActionResult Providers()
        {
            var shippingProviderModels = new List<ShippingRateComputationMethodModel>();
            var widgets = _providerManager.GetAllProviders<IShippingRateComputationMethod>();

            foreach (var widget in widgets)
            {
                var model = _moduleManager.ToProviderModel<IShippingRateComputationMethod, ShippingRateComputationMethodModel>(widget);
                model.IsActive = widget.IsShippingRateComputationMethodActive(_shippingSettings);
                shippingProviderModels.Add(model);
            }

            return View(shippingProviderModels);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Shipping.Activate)]
        public async Task<IActionResult> ActivateProvider(string systemName, bool activate)
        {
            var srcm = _providerManager.GetProvider<IShippingRateComputationMethod>(systemName);
            var isActive = srcm.Value.IsActive;

            if (!isActive && activate)
            {
                NotifyWarning(T("Admin.Configuration.Payment.CannotActivateShippingRateComputationMethod"));
            }
            else
            {
                if (!activate)
                {
                    _shippingSettings.ActiveShippingRateComputationMethodSystemNames.Remove(srcm.Metadata.SystemName);
                }
                else
                {
                    _shippingSettings.ActiveShippingRateComputationMethodSystemNames.Add(srcm.Metadata.SystemName);
                }

                await Services.SettingFactory.SaveSettingsAsync(_shippingSettings);
                await _widgetService.ActivateWidgetAsync(srcm.Metadata.SystemName, activate);
            }

            return RedirectToAction(nameof(Providers));
        }

        #endregion

        #region Shipping methods

        public IActionResult Methods()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Configuration.Shipping.Read)]
        public IActionResult List()
        {
            return View();
        }

        [Permission(Permissions.Configuration.Shipping.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new ShippingMethodModel();
            await PrepareShippingMethodModelAsync(model, null);

            AddLocales(model.Locales);
            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Configuration.Shipping.Create)]
        public async Task<IActionResult> Create(ShippingMethodModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var shippingMethod = await MapperFactory.MapAsync<ShippingMethodModel, ShippingMethod>(model);
                _db.ShippingMethods.Add(shippingMethod);
                await _db.SaveChangesAsync();

                if (model.SelectedRuleSetIds?.Any() ?? false)
                {
                    await _ruleService.ApplyRuleSetMappingsAsync(shippingMethod, model.SelectedRuleSetIds);
                }

                await SaveStoreMappingsAsync(shippingMethod, model.SelectedStoreIds);
                await UpdateLocalesAsync(shippingMethod, model);

                await _db.SaveChangesAsync();

                NotifySuccess(T("Admin.Configuration.Shipping.Methods.Added"));
                return continueEditing ? RedirectToAction(nameof(Edit), new { id = shippingMethod.Id }) : RedirectToAction(nameof(List));
            }

            return View(model);
        }

        [Permission(Permissions.Configuration.Shipping.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var shippingMethod = await _db.ShippingMethods.Include(x => x.RuleSets).FindByIdAsync(id, false);
            if (shippingMethod == null)
            {
                return NotFound();
            }

            var model = await MapperFactory.MapAsync<ShippingMethod, ShippingMethodModel>(shippingMethod);
            await PrepareShippingMethodModelAsync(model, shippingMethod);

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.Name = shippingMethod.GetLocalized(x => x.Name, languageId, false, false);
                locale.Description = shippingMethod.GetLocalized(x => x.Description, languageId, false, false);
            });

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Configuration.Shipping.Update)]
        public async Task<IActionResult> Edit(ShippingMethodModel model, bool continueEditing, IFormCollection form)
        {
            var shippingMethod = await _db.ShippingMethods.FindByIdAsync(model.Id);
            if (shippingMethod == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await MapperFactory.MapAsync(model, shippingMethod);

                // Add\remove assigned rule sets.
                await _ruleService.ApplyRuleSetMappingsAsync(shippingMethod, model.SelectedRuleSetIds);
                await SaveStoreMappingsAsync(shippingMethod, model.SelectedStoreIds);
                await UpdateLocalesAsync(shippingMethod, model);
                await _db.SaveChangesAsync();

                await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, shippingMethod, form));

                NotifySuccess(T("Admin.Configuration.Shipping.Methods.Updated"));
                return continueEditing ? RedirectToAction(nameof(Edit), new { id = shippingMethod.Id }) : RedirectToAction(nameof(List));
            }

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Shipping.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var shippingMethod = await _db.ShippingMethods.FindByIdAsync(id);
            if (shippingMethod == null)
            {
                return NotFound();
            }

            _db.ShippingMethods.Remove(shippingMethod);

            NotifySuccess(T("Admin.Configuration.Shipping.Methods.Deleted"));
            return RedirectToAction(nameof(List));
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Shipping.Read)]
        public async Task<IActionResult> ShippingMethodList(GridCommand command)
        {
            var shippingMethods = await _db.ShippingMethods
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var shippingMethodModels = await shippingMethods
                .SelectAsync(async x => 
                {
                    var model = await MapperFactory.MapAsync<ShippingMethod, ShippingMethodModel>(x);
                    model.NumberOfRules = x.RuleSets.Count;
                    model.EditUrl = Url.Action("Edit", new { id = model.Id });
                    return model;
                })
                .AsyncToList();

            var gridModel = new GridModel<ShippingMethodModel>
            {
                Rows = shippingMethodModels,
                Total = await shippingMethods.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Shipping.Delete)]
        public async Task<IActionResult> ShippingMethodDelete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var shippingMethods = await _db.ShippingMethods.GetManyAsync(ids, true);

                _db.ShippingMethods.RemoveRange(shippingMethods);

                numDeleted = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        #endregion
    }
}
