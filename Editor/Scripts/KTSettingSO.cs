using System.Collections.Generic;
using UnityEngine;

namespace com.ktgame.core.editor
{
	public class KTSettingSO : ScriptableObject
	{
		[HideInInspector] public string EdmVersion = "1.2.185";

		[HideInInspector] public string FirebaseVersion = "12.2.1";
		[HideInInspector] public IAAMediationFlag MediationFlag;

		[HideInInspector] public AnalyticsProvider AnalyticsProvider;

		[HideInInspector] public Dictionary<IAAMediationFlag, IAAFormatType> FormatMediationFlag =
			new Dictionary<IAAMediationFlag, IAAFormatType>
			{
				{ IAAMediationFlag.Max, IAAFormatType.Interstitial },
				{ IAAMediationFlag.GMA, IAAFormatType.Interstitial },
				{ IAAMediationFlag.IronSource, IAAFormatType.Interstitial }
			};
		

		[HideInInspector] public PublisherType PublisherTypeAndroid = PublisherType.INHOUSE;
		[HideInInspector] public string ProductNameAndroid;
		[HideInInspector] public string VersionNameAndroid;
		[HideInInspector] public int VersionCodeAndroid;
		[HideInInspector] public string KeystorePasswordAndroid;
		[HideInInspector] public string KeyaliasPasswordAndroid;
		
		[HideInInspector] public PublisherType PublisherTypeIos = PublisherType.INHOUSE;
		[HideInInspector] public string ProductNameIos;
		[HideInInspector] public string VersionNameIos;
		[HideInInspector] public string VersionCodeIos;
	}
	
	public enum PublisherType
	{
		ABI,
		HIGAME,
		INHOUSE,
	}
}