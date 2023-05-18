using Vintagestory.API.Common;

namespace CivMods
{
    internal struct UnderWaterGenProps
    {
		public int MinDepth;
		public int MaxDepth;
		public int MaxSections;
		public float Dampening;
		public string LiquidCode;
		public AssetLocation TopSection;
		public AssetLocation MiddleSection;
		public AssetLocation BottomSection;

        public UnderWaterGenProps(
			int minDepth = 1, 
			int maxDepth = 10,
			int maxSections = 10,
			float dampening = 0,
			string liquidCode = "water", 
			string topSection = "top", 
			string middleSection = "section", 
			string bottomSection = "section"
			)
        {
            MinDepth = minDepth;
            MaxDepth = maxDepth;
			MaxSections = maxSections;
			Dampening = dampening;
            LiquidCode = liquidCode;
            TopSection = new AssetLocation(topSection);
            MiddleSection = new AssetLocation(middleSection);
            BottomSection = new AssetLocation(bottomSection);
        }
    }
}