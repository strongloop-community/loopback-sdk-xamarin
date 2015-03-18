using System;
using NUnit.Framework;
using LBXamarinSDK;
using LBXamarinSDK.LBRepo;
namespace UnitTests
{
	/*
	 * GateWayTests
	 * 
	 * Tests some basic Gateway functions
	 */
	[TestFixture]
	public class GatewayTests
	{
		//Login
		[Test]
		public void login(){
			MiniUser credentials = new MiniUser () {
				email = "admin@g.com",
				password = "1234"
			};
			var accessToken = MiniUsers.login (credentials).Result;

			Assert.AreEqual ("1", accessToken.Property ("userId").Value.ToString ());
		}

		//Debug mode
		[Test]
		public void debugMode(){
			Assert.AreEqual (false, Gateway.GetDebugMode ());
			Gateway.SetDebugMode (true);
			Assert.AreEqual (true, Gateway.GetDebugMode ());
			Gateway.SetDebugMode (false);
			Assert.AreEqual (false, Gateway.GetDebugMode ());
		}

		//isConnected
		[Test]
		public void isConnected(){
			Gateway.SetServerBaseURL (new Uri("http://asd.com"));
			Assert.AreEqual (false, Gateway.isConnected ().Result);
			Gateway.SetServerBaseURLToSelf ();
			Assert.AreEqual (true, Gateway.isConnected ().Result);
		}
			
	}
}

