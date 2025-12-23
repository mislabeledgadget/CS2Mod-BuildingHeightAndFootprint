using System;
using BuildingHeightAndFootprint.Loader;
using Colossal.Logging;
using Colossal.UI.Binding;
using Game.UI;
using Unity.Entities;

namespace BuildingHeightAndFootprint.Systems.UI
{
    public partial class BuildingHeightAndFootprintUISystem : UISystemBase
    {
        private ILog _log;
        private GetterValueBinding<string> _statsTextBinding;
        private GetterValueBinding<string> _layoutKindBinding;

        protected override void OnCreate()
        {
            base.OnCreate();

            _log = Mod.log;
            _log.Info($"{nameof(BuildingHeightAndFootprintUISystem)}.{nameof(OnCreate)}");

            _statsTextBinding = new GetterValueBinding<string>(
                "BuildingHeightAndFootprint",
                "statsText",
                () =>
                {
                    try
                    {
                        return BuildingHeightAndFootprintLoader.GetCurrentStatsText() ?? string.Empty;
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex, $"{nameof(BuildingHeightAndFootprintUISystem)} statsText binding threw");
                        return string.Empty;
                    }
                }
            );
            AddUpdateBinding(_statsTextBinding);

            _layoutKindBinding = new GetterValueBinding<string>(
                "BuildingHeightAndFootprint",
                "layoutKind",
                () => BuildingHeightAndFootprintSystem.CurrentLayoutKind ?? string.Empty
            );
            AddUpdateBinding(_layoutKindBinding);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
        }
    }
}
