namespace com.ktgame.core
{
	[System.Flags]
	public enum IAAFormatType
	{
		Banner = 1 << 0,
		Interstitial = 1 << 1,
		Reward = 1 << 2,
		MRec = 1 << 3,
		Aoa = 1 << 4,
		Native = 1 << 5,
		NativeInterstitial = 1 << 6,
		InterstitialImage = 1 << 7,
		MRecCollapsible = 1 << 8,
		BannerCollapsible = 1 << 9,
		AoaResume = 1 << 10,
		NativeCollapsible = 1 << 11,
	}
}
