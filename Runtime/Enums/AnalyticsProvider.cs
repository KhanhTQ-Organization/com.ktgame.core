using System;

namespace com.ktgame.core
{
	[Flags]
	public enum AnalyticsProvider
	{
		Firebase = 1 << 0,
		Adjust = 1 << 1,
		AppsFlyer = 1 << 2,
	}
}
