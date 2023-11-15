using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;

namespace GroupDocs.Editor.UI.Api.Extensions;

public class ActionHidingConvention : IActionModelConvention
{
    private readonly IFeatureManager _featureManager;

    public ActionHidingConvention(IFeatureManager featureManager)
    {
        _featureManager = featureManager;
    }


    public void Apply(ActionModel action)
    {
        var featureFlag = action.Controller.Filters.FirstOrDefault(a => a.GetType() == typeof(FeatureGateAttribute)) as FeatureGateAttribute;
        var isEnable = featureFlag?.Features.Any(a => _featureManager.IsEnabledAsync(a).Result) ?? false;
        // Replace with any logic you want
        if (featureFlag != null && !isEnable)
        {
            action.ApiExplorer.IsVisible = false;
        }
    }
}