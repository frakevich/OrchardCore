using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace OrchardCore.Environment.Extensions.Features
{
    public class FeaturesProvider : IFeaturesProvider
    {
        public const string FeatureProviderCacheKey = "FeatureProvider:Features";

        private readonly IEnumerable<IFeatureBuilderEvents> _featureBuilderEvents;

        private readonly ILogger L;

        public FeaturesProvider(
            IEnumerable<IFeatureBuilderEvents> featureBuilderEvents,
            ILogger<FeaturesProvider> logger)
        {
            _featureBuilderEvents = featureBuilderEvents;
            L = logger;
        }

        public IEnumerable<IFeatureInfo> GetFeatures(
            IExtensionInfo extensionInfo,
            IManifestInfo manifestInfo)
        {
            var featuresInfos = new List<IFeatureInfo>();

            // Features and Dependencies live within this section
            var features = manifestInfo.ModuleInfo.Features.ToList();
            if (features.Count > 0)
            {
                foreach (var feature in features.Where(f => f.Exists))
                {
                    var featureId = feature.Id;
                    var featureName = feature.Name ?? feature.Id;

                    var featureDependencyIds = feature.Dependencies
                            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(e => e.Trim())
                            .ToArray();

                    if (!int.TryParse(feature.Priority ?? manifestInfo.ModuleInfo.Priority, out int featurePriority))
                    {
                        featurePriority = 0;
                    }

                    var featureCategory = feature.Category ?? manifestInfo.ModuleInfo.Category;
                    var featureDescription = feature.Description ?? manifestInfo.ModuleInfo.Description;

                    var context = new FeatureBuildingContext
                    {
                        FeatureId = featureId,
                        FeatureName = featureName,
                        Category = featureCategory,
                        Description = featureDescription,
                        ExtensionInfo = extensionInfo,
                        ManifestInfo = manifestInfo,
                        Priority = featurePriority,
                        FeatureDependencyIds = featureDependencyIds
                    };

                    foreach (var builder in _featureBuilderEvents)
                    {
                        builder.Building(context);
                    }

                    var featureInfo = new FeatureInfo(
                        featureId,
                        featureName,
                        featurePriority,
                        featureCategory,
                        featureDescription,
                        extensionInfo,
                        featureDependencyIds);

                    foreach (var builder in _featureBuilderEvents)
                    {
                        builder.Built(featureInfo);
                    }
                    
                    featuresInfos.Add(featureInfo);
                }
            }
            else
            {
                // The Extension has only one feature, itself, and that can have dependencies
                var featureId = extensionInfo.Id;
                var featureName = manifestInfo.Name;

                var featureDependencyIds = manifestInfo.ModuleInfo.Dependencies
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(e => e.Trim())
                        .ToArray();

                if (!int.TryParse(manifestInfo.ModuleInfo.Priority, out int featurePriority))
                {
                    featurePriority = 0;
                }

                var featureCategory = manifestInfo.ModuleInfo.Category;
                var featureDescription = manifestInfo.ModuleInfo.Description;

                var context = new FeatureBuildingContext
                {
                    FeatureId = featureId,
                    FeatureName = featureName,
                    Category = featureCategory,
                    Description = featureDescription,
                    ExtensionInfo = extensionInfo,
                    ManifestInfo = manifestInfo,
                    Priority = featurePriority,
                    FeatureDependencyIds = featureDependencyIds
                };

                foreach (var builder in _featureBuilderEvents)
                {
                    builder.Building(context);
                }

                var featureInfo = new FeatureInfo(
                    context.FeatureId,
                    context.FeatureName,
                    context.Priority,
                    context.Category,
                    context.Description,
                    context.ExtensionInfo,
                    context.FeatureDependencyIds);

                foreach (var builder in _featureBuilderEvents)
                {
                    builder.Built(featureInfo);
                }

                featuresInfos.Add(featureInfo);
            }

            return featuresInfos;
        }
    }
}
