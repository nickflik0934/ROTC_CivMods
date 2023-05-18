using Vintagestory.API.Client;

namespace CivMods
{
    public class HudElementAirBar : HudElement
    {
        private GuiElementStatbar statbar;

        public HudElementAirBar(ICoreClientAPI capi) : base(capi)
        {
        }

        public override void OnOwnPlayerDataReceived()
        {
            ElementBounds statbarbounds = ElementStdBounds.Statbar(EnumDialogArea.CenterBottom, 347).WithFixedAlignmentOffset(-125, -44);
            statbarbounds.WithFixedHeight(9);

            ElementBounds parentBounds = statbarbounds.ForkBoundingParent();

            SingleComposer = capi.Gui.CreateCompo("airbar", parentBounds)
                .AddStatbar(statbarbounds, new double[] { 0.2, 0.2, 0.2, 0.5 }, "background")
                .AddStatbar(statbarbounds, new double[] { 255.0 / 66.0, 255.0 / 134.0, 255.0 / 244.0, 0.5 }, "airbar")
                .Compose();
            SingleComposer.GetStatbar("background").SetMinMax(0, 1);

            statbar = SingleComposer.GetStatbar("airbar");
            statbar.SetMinMax(0, 1);
            statbar.SetLineInterval(1f / 16f);
            statbar.FlashTime = 4.0f;

            SingleComposer.ReCompose();

            capi.World.Player.Entity.WatchedAttributes.RegisterModifiedListener("currentAir", () => UpdateGUI());

            base.OnOwnPlayerDataReceived();
        }

        public void UpdateGUI()
        {
            float? currentAir = capi.World.Player.Entity.GetBehavior<EntityBehaviorSuffocate>().CurrentAir;
            if (currentAir != null && currentAir != statbar.GetValue())
            {
                statbar.SetValue((float)currentAir);
            }
        }

        public override void OnRenderGUI(float deltaTime)
        {
            statbar.ShouldFlash = statbar.GetValue() < 0.5 ? true : false;
            base.OnRenderGUI(deltaTime);
        }
    }
}